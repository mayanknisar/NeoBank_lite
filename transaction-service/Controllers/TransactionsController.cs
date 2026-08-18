using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using TransactionService.Data;
using TransactionService.Grpc;
using TransactionService.Models;

namespace TransactionService.Controllers;

[ApiController]
[Route("api")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionRepository _repo;
    private readonly AccountGrpcClient _accountClient;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(TransactionRepository repo, AccountGrpcClient accountClient, ILogger<TransactionsController> logger)
    {
        _repo = repo;
        _accountClient = accountClient;
        _logger = logger;
    }

    public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string IdempotencyKey);
    public record DepositRequest(Guid ToAccountId, decimal Amount, string IdempotencyKey);
    public record WithdrawRequest(Guid FromAccountId, decimal Amount, string IdempotencyKey);

    [HttpPost("transactions/transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
    {
        if (req.Amount <= 0) return BadRequest(Error("INVALID_REQUEST", "Amount must be greater than zero."));

        // Idempotency check first — a retried request with the same key
        // returns the original result instead of processing twice.
        var existing = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey);
        if (existing is not null) return Ok(ToResponse(existing));

        AccountResponseGuid source;
        try
        {
            var account = await _accountClient.GetAccountAsync(req.FromAccountId.ToString());
            source = new(Guid.Parse(account.CustomerId));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return NotFound(Error("ACCOUNT_NOT_FOUND", "Source account not found."));
        }

        var transactionId = await _repo.CreatePendingAsync(req.FromAccountId, req.ToAccountId, req.Amount, "TRANSFER", req.IdempotencyKey);

        var debit = await _accountClient.DebitAsync(req.FromAccountId.ToString(), req.Amount, req.IdempotencyKey, transactionId.ToString());
        if (!debit.Success)
        {
            await _repo.FailAsync(transactionId, source.CustomerId, req.Amount, debit.Message);
            return BadRequest(new { success = false, message = debit.Message, transactionId });
        }

        var credit = await _accountClient.CreditAsync(req.ToAccountId.ToString(), req.Amount, req.IdempotencyKey, transactionId.ToString());
        if (!credit.Success)
        {
            // Compensating action: the debit already succeeded, so refund the
            // source rather than leaving money debited with nowhere credited.
            // This is a manual saga step — fine for a learning project, but a
            // production system would likely drive this from a durable
            // workflow engine so a crash mid-compensation can still recover.
            _logger.LogWarning("Credit failed after debit for txn {TransactionId}, reversing debit", transactionId);
            await _accountClient.CreditAsync(req.FromAccountId.ToString(), req.Amount, $"{req.IdempotencyKey}-reversal", transactionId.ToString());
            await _repo.FailAsync(transactionId, source.CustomerId, req.Amount, $"Credit failed, debit reversed: {credit.Message}");
            return BadRequest(new { success = false, message = "Transfer failed and was reversed.", transactionId });
        }

        await _repo.CompleteAsync(transactionId, source.CustomerId, req.FromAccountId, req.ToAccountId, req.Amount, "INR", "TRANSFER");
        var completed = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey);
        return Ok(ToResponse(completed!));
    }

    [HttpPost("transactions/deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest req)
    {
        if (req.Amount <= 0) return BadRequest(Error("INVALID_REQUEST", "Amount must be greater than zero."));

        var existing = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey);
        if (existing is not null) return Ok(ToResponse(existing));

        AccountResponseGuid target;
        try
        {
            var account = await _accountClient.GetAccountAsync(req.ToAccountId.ToString());
            target = new(Guid.Parse(account.CustomerId));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return NotFound(Error("ACCOUNT_NOT_FOUND", "Target account not found."));
        }

        var transactionId = await _repo.CreatePendingAsync(null, req.ToAccountId, req.Amount, "DEPOSIT", req.IdempotencyKey);

        var credit = await _accountClient.CreditAsync(req.ToAccountId.ToString(), req.Amount, req.IdempotencyKey, transactionId.ToString());
        if (!credit.Success)
        {
            await _repo.FailAsync(transactionId, target.CustomerId, req.Amount, credit.Message);
            return BadRequest(new { success = false, message = credit.Message, transactionId });
        }

        await _repo.CompleteAsync(transactionId, target.CustomerId, null, req.ToAccountId, req.Amount, "INR", "DEPOSIT");
        var completed = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey);
        return Ok(ToResponse(completed!));
    }

    [HttpPost("transactions/withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest req)
    {
        if (req.Amount <= 0) return BadRequest(Error("INVALID_REQUEST", "Amount must be greater than zero."));

        var existing = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey);
        if (existing is not null) return Ok(ToResponse(existing));

        AccountResponseGuid source;
        try
        {
            var account = await _accountClient.GetAccountAsync(req.FromAccountId.ToString());
            source = new(Guid.Parse(account.CustomerId));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return NotFound(Error("ACCOUNT_NOT_FOUND", "Source account not found."));
        }

        var transactionId = await _repo.CreatePendingAsync(req.FromAccountId, null, req.Amount, "WITHDRAWAL", req.IdempotencyKey);

        var debit = await _accountClient.DebitAsync(req.FromAccountId.ToString(), req.Amount, req.IdempotencyKey, transactionId.ToString());
        if (!debit.Success)
        {
            await _repo.FailAsync(transactionId, source.CustomerId, req.Amount, debit.Message);
            return BadRequest(new { success = false, message = debit.Message, transactionId });
        }

        await _repo.CompleteAsync(transactionId, source.CustomerId, req.FromAccountId, null, req.Amount, "INR", "WITHDRAWAL");
        var completed = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey);
        return Ok(ToResponse(completed!));
    }

    [HttpGet("accounts/{accountId:guid}/transactions")]
    public async Task<IActionResult> GetHistory(Guid accountId)
    {
        var history = await _repo.GetHistoryForAccountAsync(accountId);
        return Ok(history);
    }

    private static object ToResponse(Transaction t) => new
    {
        t.TransactionId,
        t.FromAccountId,
        t.ToAccountId,
        t.Amount,
        t.Currency,
        t.Type,
        t.Status,
        t.FailureReason,
        t.CreatedAt,
        t.CompletedAt,
        success = t.Status == "COMPLETED"
    };

    private static object Error(string code, string message) =>
        new { error = new { code, message, traceId = Guid.NewGuid().ToString() } };

    private record AccountResponseGuid(Guid CustomerId);
}
