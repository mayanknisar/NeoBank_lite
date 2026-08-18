using Microsoft.AspNetCore.Mvc;
using AccountService.Data;
using AccountService.Caching;
using AccountService.Models;
using System.Text.Json;

namespace AccountService.Controllers;

[ApiController]
[Route("api")]
public class AccountsController : ControllerBase
{
    private readonly AccountRepository _repo;
    private readonly AccountCacheService _cache;

    public AccountsController(AccountRepository repo, AccountCacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    [HttpGet("accounts/{accountId:guid}")]
    public async Task<IActionResult> GetAccount(Guid accountId)
    {
        var account = await _repo.GetAccountAsync(accountId);
        if (account is null) return NotFound(Error("ACCOUNT_NOT_FOUND", "Account not found."));
        return Ok(account);
    }

    [HttpGet("accounts/{accountId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid accountId)
    {
        var account = await _repo.GetAccountAsync(accountId);
        await _cache.GetAccountWithCachedBalanceAsync(accountId);
        if (account is null)
            return NotFound(Error("ACCOUNT_NOT_FOUND", "Account not found."));
        return Ok(new { account.AccountId, account.Balance, account.Version });
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _repo.GetCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("customers/{customerId:guid}/accounts")]
    public async Task<IActionResult> GetCustomerAccounts(Guid customerId)
    {
        var accounts = await _repo.GetAccountsForCustomerAsync(customerId);
        return Ok(accounts);
    }

    [HttpPost("accounts/{accountId:guid}/debit")]
    public async Task<IActionResult> DebitAccount(Guid accountId, [FromBody] decimal amount)
    {
        var current = await _repo.GetAccountAsync(accountId);
        if (current is null) return NotFound(Error("ACCOUNT_NOT_FOUND", "Account not found."));

        var updated = await _repo.DebitAsync(accountId, amount, current.Version);
        if (updated is null)
        {
            var reCheck = await _repo.GetAccountAsync(accountId);
            var reason = reCheck is null
                ? "Account not found"
                : reCheck.Balance < amount
                    ? "Insufficient funds"
                    : "Concurrent update detected — retry";

            return BadRequest(new { success = false, message = reason });
        }

        return Ok(new { success = true, newBalance = updated.Balance, version = updated.Version });
    }

    [HttpPost("accounts/{accountId:guid}/credit")]
    public async Task<IActionResult> CreditAccount(Guid accountId, [FromBody] decimal amount)
    {
        var current = await _repo.GetAccountAsync(accountId);
        if (current is null) return NotFound(Error("ACCOUNT_NOT_FOUND", "Account not found."));

        var updated = await _repo.CreditAsync(accountId, amount, current.Version);
        if (updated is null)
        {
            return BadRequest(new { success = false, message = "Concurrent update detected — retry" });
        }

        return Ok(new { success = true, newBalance = updated.Balance, version = updated.Version });
    }

    [HttpGet("customers/{customerId:guid}/kyc")]
    public async Task<IActionResult> GetKycStatus(Guid customerId)
    {
        var kyc = await _repo.GetKycStatusAsync(customerId);
        if (kyc is null) return NotFound(Error("KYC_NOT_FOUND", "KYC status not found."));
        return Ok(kyc);
    }

    [HttpGet("accounts/{accountId:guid}/average-balance")]
    public async Task<IActionResult> GetAverageBalance(Guid accountId)
    {
        var avg = await _repo.GetAverageBalanceLast90DaysAsync(accountId);
        return Ok(new { accountId, averageBalanceLast90Days = avg });
    }


    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
    {
        var customerId = await _repo.CreateCustomerAsync(customer);
        if (customerId is null) return BadRequest(Error("CUSTOMER_CREATION_FAILED", "Failed to create customer."));
        return CreatedAtAction(nameof(GetCustomerAccounts), new { customerId }, new { customerId });
    }

    [HttpPost("customers/{customerId:guid}/accounts")]
    public async Task<IActionResult> CreateAccount(Guid customerId, [FromBody] AccountDTO account)
    {
        var accId = await _repo.CreateAccountAsync(account, customerId);
        if (accId is null) return BadRequest(Error("ACCOUNT_CREATION_FAILED", "Failed to create account."));
        return CreatedAtAction(nameof(GetAccount), new { accountId = accId }, new { accountId = accId });
    }

    private static object Error(string code, string message) =>
        new { error = new { code, message, traceId = Guid.NewGuid().ToString() } };
}
