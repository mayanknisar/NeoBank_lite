using Grpc.Core;
using NeoBank.Account.Grpc;
using AccountService.Data;
using AccountService.Caching;

namespace AccountService.Grpc;

public class AccountGrpcServiceImpl : NeoBank.Account.Grpc.AccountService.AccountServiceBase
{
    private readonly AccountRepository _repo;
    private readonly AccountCacheService _cache;
    private readonly ILogger<AccountGrpcServiceImpl> _logger;

    public AccountGrpcServiceImpl(AccountRepository repo, AccountCacheService cache, ILogger<AccountGrpcServiceImpl> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<AccountResponse> GetAccount(GetAccountRequest request, ServerCallContext context)
    {
        var account = await _repo.GetAccountAsync(Guid.Parse(request.AccountId));
        if (account is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Account {request.AccountId} not found"));

        return new AccountResponse
        {
            AccountId = account.AccountId.ToString(),
            CustomerId = account.CustomerId.ToString(),
            AccountNumber = account.AccountNumber,
            AccountType = account.AccountType,
            Balance = (double)account.Balance,
            Status = account.Status
        };
    }

    public override async Task<BalanceResponse> GetBalance(GetBalanceRequest request, ServerCallContext context)
    {
        var accountId = Guid.Parse(request.AccountId);
        var account = await _cache.GetAccountWithCachedBalanceAsync(accountId);
        if (account is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Account {request.AccountId} not found"));

        return new BalanceResponse
        {
            AccountId = account.AccountId.ToString(),
            Balance = (double)account.Balance,
            Version = account.Version
        };
    }

    // Called synchronously by Transaction Service before it marks a transfer
    // COMPLETED. Uses optimistic locking (see AccountRepository.DebitAsync) —
    // on a version conflict this returns success = false so the caller can
    // retry the whole transaction rather than silently double-spending.
    public override async Task<LedgerOpResponse> DebitAccount(DebitRequest request, ServerCallContext context)
    {
        var accountId = Guid.Parse(request.AccountId);
        var current = await _repo.GetAccountAsync(accountId);
        if (current is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Account {request.AccountId} not found"));

        var updated = await _repo.DebitAsync(accountId, (decimal)request.Amount, current.Version);
        await _cache.InvalidateAsync(accountId);

        if (updated is null)
        {
            // Re-check to give the caller a precise reason instead of a generic failure.
            var reCheck = await _repo.GetAccountAsync(accountId);
            var reason = reCheck is null
                ? "Account not found"
                : reCheck.Balance < (decimal)request.Amount
                    ? "Insufficient funds"
                    : "Concurrent update detected — retry";

            _logger.LogWarning("Debit failed for {AccountId}: {Reason}", accountId, reason);
            return new LedgerOpResponse { Success = false, Message = reason };
        }

        return new LedgerOpResponse
        {
            Success = true,
            NewBalance = (double)updated.Balance,
            Version = updated.Version
        };
    }

    public override async Task<LedgerOpResponse> CreditAccount(CreditRequest request, ServerCallContext context)
    {
        var accountId = Guid.Parse(request.AccountId);
        var current = await _repo.GetAccountAsync(accountId);
        if (current is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Account {request.AccountId} not found"));

        var updated = await _repo.CreditAsync(accountId, (decimal)request.Amount, current.Version);
        await _cache.InvalidateAsync(accountId);

        if (updated is null)
        {
            _logger.LogWarning("Credit failed for {AccountId}: concurrent update", accountId);
            return new LedgerOpResponse { Success = false, Message = "Concurrent update detected — retry" };
        }

        return new LedgerOpResponse
        {
            Success = true,
            NewBalance = (double)updated.Balance,
            Version = updated.Version
        };
    }

    // Called synchronously by Loan Service before approving a loan.
    public override async Task<StandingResponse> CheckAccountStanding(StandingRequest request, ServerCallContext context)
    {
        var accountId = Guid.Parse(request.AccountId);
        var account = await _repo.GetAccountAsync(accountId);
        if (account is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Account {request.AccountId} not found"));

        var kyc = await _repo.GetKycStatusAsync(account.CustomerId);
        var avgBalance = await _repo.GetAverageBalanceLast90DaysAsync(accountId);

        return new StandingResponse
        {
            InGoodStanding = account.Status == "ACTIVE" && kyc?.Status == "VERIFIED",
            KycStatus = kyc?.Status ?? "PENDING",
            AverageBalanceLast90Days = (double)avgBalance
        };
    }
}
