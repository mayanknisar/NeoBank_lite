using AccountService.Models;
using Microsoft.EntityFrameworkCore;

public class SqliteDbService : IDatabaseService
{
    private readonly SqliteDbContext _dbContext;

    public SqliteDbService(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreated();
    }

    public Task<Account?> CreditAsync(Guid accountId, decimal amount, long expectedVersion)
    {
        _dbContext.ChangeTracker.Clear(); // Clear the change tracker to avoid tracking issues
        var account = _dbContext.Accounts.SingleOrDefault(a => a.AccountId == accountId && a.Version == expectedVersion);
        if (account is null) return Task.FromResult<Account?>(null);
        account.Balance += amount;
        account.Version++;
        _dbContext.SaveChanges();
        return Task.FromResult<Account?>(account);
    }

    public Task<Account?> DebitAsync(Guid accountId, decimal amount, long expectedVersion)
    {
        _dbContext.ChangeTracker.Clear(); // Clear the change tracker to avoid tracking issues
        var account = _dbContext.Accounts.SingleOrDefault(a => a.AccountId == accountId && a.Version == expectedVersion);
        if (account is null) return Task.FromResult<Account?>(null);
        account.Balance -= amount;
        account.Version++;
        return Task.FromResult<Account?>(account);
    }

    public Task<IEnumerable<Customer>> GetCustomersAsync()
    {
        _dbContext.ChangeTracker.Clear(); // Clear the change tracker to avoid tracking issues
        return _dbContext.Customers.ToListAsync().ContinueWith(t => (IEnumerable<Customer>)t.Result);
    }

    public Task<Account?> GetAccountAsync(Guid accountId)
    {
        return _dbContext.Accounts
            .SingleOrDefaultAsync(a => a.AccountId == accountId);
    }

    public Task<IEnumerable<Account>> GetAccountsForCustomerAsync(Guid customerId)
    {
        _dbContext.ChangeTracker.Clear(); // Clear the change tracker to avoid tracking issues
        return _dbContext.Accounts
            .Where(a => a.CustomerId == customerId).ToListAsync().ContinueWith(t => (IEnumerable<Account>)t.Result);
    }

    public Task<KycStatus?> GetKycStatusAsync(Guid customerId)
    {
        _dbContext.ChangeTracker.Clear(); // Clear the change tracker to avoid tracking issues
        return _dbContext.KycStatuses
            .SingleOrDefaultAsync(k => k.CustomerId == customerId);
    }

    public Task<decimal> GetAverageBalanceLast90DaysAsync(Guid accountId)
    {
        _dbContext.ChangeTracker.Clear(); // Clear the change tracker to avoid tracking issues
        return _dbContext.Accounts
            .Where(a => a.AccountId == accountId)
            .Select(a => a.Balance)
            .AverageAsync();
    }

    public async Task<Guid?> CreateCustomerAsync(Customer customer)
    {
        try
        {
            if (customer.CustomerId == Guid.Empty)
            {
                customer.CustomerId = Guid.NewGuid();
            }

            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                CustomerId = customer.CustomerId,
                AccountNumber = GenerateAccountNumber(),
                AccountType = "SAVINGS",
                Balance = 5000m,
                Status = "ACTIVE",
                Version = 1
            };

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();

            _dbContext.Accounts.Add(account);

            var changes = await _dbContext.SaveChangesAsync();
            return changes > 0 ? customer.CustomerId : (Guid?)null;
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
            Console.WriteLine($"Error creating customer and account: {ex.Message}");
            return null;

        }
    }

    public async Task<Guid?> CreateAccountAsync(AccountDTO accountdto, Guid customerId)
    {
        try
        {
            if (!_dbContext.Customers.Any(c => c.CustomerId == customerId))
                throw new ArgumentException("Customer not found", nameof(customerId));
            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                CustomerId = customerId,
                AccountNumber = GenerateAccountNumber(),
                AccountType = accountdto.AccountType,
                Balance = accountdto.Balance,
                Status = "ACTIVE",
                Version = 1
            };

            _dbContext.Accounts.Add(account);
            var changes = await _dbContext.SaveChangesAsync();
            return changes > 0 ? account.AccountId : (Guid?)null;
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
            Console.WriteLine($"Error creating account: {ex.Message}");
            return null;
        }
    }

    private static string GenerateAccountNumber()
    {
        var candidate = $"ACC{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}";
        return candidate.Length <= 20 ? candidate : candidate.Substring(0, 20);
    }
}