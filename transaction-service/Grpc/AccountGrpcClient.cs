using Grpc.Net.Client;
using NeoBank.Account.Grpc;

namespace TransactionService.Grpc;

/// <summary>
/// Wraps the generated gRPC client for calls into Account Service. This is
/// the synchronous side of the architecture — a transfer cannot be marked
/// COMPLETED until the debit here actually succeeds, so these calls are
/// awaited inline rather than fired-and-forgotten like the Kafka events.
///
/// Known gap: account.proto's DebitRequest/CreditRequest carry an
/// idempotency_key, but Account Service's current implementation doesn't
/// check it — it only re-validates the optimistic-lock version. That means
/// a network retry of this exact call (e.g. response lost after the debit
/// already applied) could double-debit. A real implementation would need
/// Account Service to persist idempotency keys server-side before applying
/// the ledger change, not just accept the field.
/// </summary>
public class AccountGrpcClient
{
    private readonly NeoBank.Account.Grpc.AccountService.AccountServiceClient _client;

    public AccountGrpcClient(IConfiguration config)
    {
        var address = config["AccountService:GrpcAddress"] ?? "http://localhost:6001";
        var channel = GrpcChannel.ForAddress(address);
        _client = new NeoBank.Account.Grpc.AccountService.AccountServiceClient(channel);
    }

    public Task<AccountResponse> GetAccountAsync(string accountId) =>
        _client.GetAccountAsync(new GetAccountRequest { AccountId = accountId }).ResponseAsync;

    public Task<LedgerOpResponse> DebitAsync(string accountId, decimal amount, string idempotencyKey, string referenceTransactionId) =>
        _client.DebitAccountAsync(new DebitRequest
        {
            AccountId = accountId,
            Amount = (double)amount,
            IdempotencyKey = idempotencyKey,
            ReferenceTransactionId = referenceTransactionId
        }).ResponseAsync;

    public Task<LedgerOpResponse> CreditAsync(string accountId, decimal amount, string idempotencyKey, string referenceTransactionId) =>
        _client.CreditAccountAsync(new CreditRequest
        {
            AccountId = accountId,
            Amount = (double)amount,
            IdempotencyKey = idempotencyKey,
            ReferenceTransactionId = referenceTransactionId
        }).ResponseAsync;
}
