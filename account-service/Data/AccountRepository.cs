using Dapper;
using Npgsql;
using AccountService.Models;

namespace AccountService.Data;

public class
AccountRepository
{
    private readonly IDatabaseService _dbService;

    public AccountRepository(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<Account?> GetAccountAsync(Guid accountId)
    {
        return await _dbService.GetAccountAsync(accountId);
    }

    public async Task<IEnumerable<Account>> GetAccountsForCustomerAsync(Guid customerId)
    {
        return await _dbService.GetAccountsForCustomerAsync(customerId);
    }

    public async Task<KycStatus?> GetKycStatusAsync(Guid customerId)
    {
        return await _dbService.GetKycStatusAsync(customerId);
    }

    /// <summary>
    /// Debits an account using optimistic locking. Returns null if the
    /// version has moved on since the caller last read it (i.e. a
    /// concurrent write beat us to it) — the gRPC layer treats that as a
    /// "retry" signal, not a hard failure.
    /// </summary>
    public async Task<Account?> DebitAsync(Guid accountId, decimal amount, long expectedVersion)
    {
        return await _dbService.DebitAsync(accountId, amount, expectedVersion);
    }

    public async Task<Account?> CreditAsync(Guid accountId, decimal amount, long expectedVersion)
    {
        return await _dbService.CreditAsync(accountId, amount, expectedVersion);
    }

    public async Task<decimal> GetAverageBalanceLast90DaysAsync(Guid accountId)
    {
        return await _dbService.GetAverageBalanceLast90DaysAsync(accountId);
    }

    public async Task<Guid?> CreateCustomerAsync(Customer customer)
    {
        return await _dbService.CreateCustomerAsync(customer);
    }
}
