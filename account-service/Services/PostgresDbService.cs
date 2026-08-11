using AccountService.Models;
using Dapper;
using Npgsql;

public class PostgresDbService : IDatabaseService
{
    private readonly string _connectionString;

    public PostgresDbService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string not configured");
    }
    private NpgsqlConnection Connection() => new(_connectionString);

    public async Task<Account?> CreditAsync(Guid accountId, decimal amount, long expectedVersion)
    {
        using var conn = Connection();
        return await conn.QuerySingleOrDefaultAsync<Account>(
            """
            UPDATE accounts
            SET balance = balance + @amount, version = version + 1, updated_at = now()
            WHERE account_id = @accountId AND version = @expectedVersion
            RETURNING account_id AS AccountId, customer_id AS CustomerId,
                      account_number AS AccountNumber, account_type AS AccountType,
                      balance AS Balance, status AS Status, version AS Version
            """,
            new { accountId, amount, expectedVersion });
    }

    public async Task<Account?> DebitAsync(Guid accountId, decimal amount, long expectedVersion)
    {
        using var conn = Connection();
        return await conn.QuerySingleOrDefaultAsync<Account>(
            """
            UPDATE accounts
            SET balance = balance - @amount, version = version + 1, updated_at = now()
            WHERE account_id = @accountId
              AND version = @expectedVersion
              AND balance >= @amount
            RETURNING account_id AS AccountId, customer_id AS CustomerId,
                      account_number AS AccountNumber, account_type AS AccountType,
                      balance AS Balance, status AS Status, version AS Version
            """,
            new { accountId, amount, expectedVersion });
    }

    public async Task<Account?> GetAccountAsync(Guid accountId)
    {

        using var conn = Connection();
        return await conn.QuerySingleOrDefaultAsync<Account>(
            """
            SELECT account_id AS AccountId, customer_id AS CustomerId,
                   account_number AS AccountNumber, account_type AS AccountType,
                   balance AS Balance, status AS Status, version AS Version
            FROM accounts WHERE account_id = @accountId
            """,
             new { accountId });
    }

    public async Task<IEnumerable<Account>> GetAccountsForCustomerAsync(Guid customerId)
    {
        using var conn = Connection();
        return await conn.QueryAsync<Account>(
            """
            SELECT account_id AS AccountId, customer_id AS CustomerId,
                   account_number AS AccountNumber, account_type AS AccountType,
                   balance AS Balance, status AS Status, version AS Version
            FROM accounts WHERE customer_id = @customerId
            """,
            new { customerId });
    }

    public async Task<KycStatus?> GetKycStatusAsync(Guid customerId)
    {
        using var conn = Connection();
        return await conn.QuerySingleOrDefaultAsync<KycStatus>(
            """
            SELECT kyc_id AS KycId, customer_id AS CustomerId,
                   status AS Status, verified_at AS VerifiedAt
            FROM kyc_status WHERE customer_id = @customerId
            ORDER BY created_at DESC LIMIT 1
            """,
            new { customerId });
    }

    public async Task<decimal> GetAverageBalanceLast90DaysAsync(Guid accountId)
    {
        // Simplified for the learning project: real version would read a
        // balance-history/snapshot table. Here we just return current balance.
        using var conn = Connection();
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT balance FROM accounts WHERE account_id = @accountId",
            new { accountId });
    }

    public async Task<Guid?> CreateCustomerAsync(Customer customer)
    {
        customer.CustomerId = customer.CustomerId == Guid.Empty ? Guid.NewGuid() : customer.CustomerId;

        using var conn = Connection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        await conn.ExecuteAsync(
            """
            INSERT INTO customers (customer_id, full_name, email, phone, date_of_birth, created_at, updated_at)
            VALUES (@CustomerId, @FullName, @Email, @Phone, @DateOfBirth, now(), now())
            """,
            customer,
            tx);

        var accountNumber = GenerateAccountNumber();
        await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO accounts (account_id, customer_id, account_number, account_type, balance, status, version, created_at, updated_at)
            VALUES (@AccountId, @CustomerId, @AccountNumber, 'SAVINGS', 5000, 'ACTIVE', 1, now(), now())
            RETURNING account_id
            """,
            new
            {
                AccountId = Guid.NewGuid(),
                customer.CustomerId,
                AccountNumber = accountNumber
            },
            tx);

        await tx.CommitAsync();
        return customer.CustomerId;
    }

    private static string GenerateAccountNumber()
    {
        return $"ACC{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}".Substring(0, 20);
    }
}
