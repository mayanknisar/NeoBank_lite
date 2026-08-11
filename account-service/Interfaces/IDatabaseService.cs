using AccountService.Models;

public interface IDatabaseService
{
    Task<Account?> GetAccountAsync(Guid accountId);
    Task<IEnumerable<Account>> GetAccountsForCustomerAsync(Guid customerId);
    Task<KycStatus?> GetKycStatusAsync(Guid customerId);
    Task<Account?> DebitAsync(Guid accountId, decimal amount, long expectedVersion);
    Task<Account?> CreditAsync(Guid accountId, decimal amount, long expectedVersion);

    Task<decimal> GetAverageBalanceLast90DaysAsync(Guid accountId);

    Task<Guid?> CreateCustomerAsync(Customer customer);
}