namespace AccountService.Models;

public class Customer
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
}

public class Account
{
    public Guid AccountId { get; set; }
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = default!;
    public string AccountType { get; set; } = default!;
    public decimal Balance { get; set; }
    public string Status { get; set; } = default!;
    // Optimistic-lock version. Every debit/credit must supply the version
    // it read and the UPDATE only succeeds if it still matches — this is
    // what stops two concurrent transfers from double-spending a balance.
    public long Version { get; set; }
}

public class KycStatus
{
    public Guid KycId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public DateTime? VerifiedAt { get; set; }
}

public record AccountDTO(string AccountType, decimal Balance);