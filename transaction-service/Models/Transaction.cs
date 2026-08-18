namespace TransactionService.Models;

public class Transaction
{
    public Guid TransactionId { get; set; }
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Type { get; set; } = default!;       // TRANSFER | DEPOSIT | WITHDRAWAL
    public string Status { get; set; } = default!;      // PENDING | COMPLETED | FAILED | REVERSED
    public string IdempotencyKey { get; set; } = default!;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
