using System.Text.Json;
using StackExchange.Redis;
using AccountService.Models;
using AccountService.Data;

namespace AccountService.Caching;

/// <summary>
/// Cache-aside pattern for balance reads: check Redis first, fall back to
/// Postgres on a miss, then populate Redis with a short TTL. Balance
/// changes often, so we keep the TTL tight (30s) rather than trying to
/// keep the cache perfectly in sync — a stale read here is low-stakes
/// since the debit/credit path always re-reads from Postgres directly.
/// </summary>
public class AccountCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly AccountRepository _repo;
    private static readonly TimeSpan BalanceTtl = TimeSpan.FromSeconds(30);

    public AccountCacheService(IConnectionMultiplexer redis, AccountRepository repo)
    {
        _redis = redis;
        _repo = repo;
    }

    private static string BalanceKey(Guid accountId) => $"account:balance:{accountId}";

    public async Task<Account?> GetAccountWithCachedBalanceAsync(Guid accountId)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(BalanceKey(accountId));

        if (cached.HasValue)
        {
            var account = JsonSerializer.Deserialize<Account>((string)cached!);
            if (account is not null) return account;
        }

        var fresh = await _repo.GetAccountAsync(accountId);
        if (fresh is not null)
        {
            await db.StringSetAsync(BalanceKey(accountId), JsonSerializer.Serialize(fresh), BalanceTtl);
        }
        return fresh;
    }

    /// <summary>Call after any debit/credit so the next read isn't stale for up to 30s.</summary>
    public async Task InvalidateAsync(Guid accountId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BalanceKey(accountId));
    }
}
