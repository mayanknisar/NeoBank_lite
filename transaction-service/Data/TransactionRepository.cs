using System.Text.Json;
using Dapper;
using Npgsql;
using TransactionService.Models;

namespace TransactionService.Data;

public class TransactionRepository
{
    private readonly string _connString;

    public TransactionRepository(IConfiguration config)
    {
        _connString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string not configured");
    }

    private NpgsqlConnection Connection() => new(_connString);

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        using var conn = Connection();
        return await conn.QuerySingleOrDefaultAsync<Transaction>(
            """
            SELECT transaction_id AS TransactionId, from_account_id AS FromAccountId,
                   to_account_id AS ToAccountId, amount AS Amount, currency AS Currency,
                   type AS Type, status AS Status, idempotency_key AS IdempotencyKey,
                   failure_reason AS FailureReason, created_at AS CreatedAt, completed_at AS CompletedAt
            FROM transactions WHERE idempotency_key = @idempotencyKey
            """,
            new { idempotencyKey });
    }

    public async Task<Guid> CreatePendingAsync(Guid? fromAccountId, Guid? toAccountId, decimal amount, string type, string idempotencyKey)
    {
        using var conn = Connection();
        return await conn.QuerySingleAsync<Guid>(
            """
            INSERT INTO transactions (from_account_id, to_account_id, amount, type, status, idempotency_key)
            VALUES (@fromAccountId, @toAccountId, @amount, @type, 'PENDING', @idempotencyKey)
            RETURNING transaction_id
            """,
            new { fromAccountId, toAccountId, amount, type, idempotencyKey });
    }

    /// <summary>
    /// Marks the transaction COMPLETED, writes the ledger entries, and writes
    /// the outbox event — all in one local DB transaction. This is the actual
    /// outbox pattern: the event row commits atomically with the state
    /// change it describes, so there's no window where the ledger says
    /// "done" but the event never gets written (or vice versa). A separate
    /// poller (see OutboxPublisher) reads unpublished rows and pushes them
    /// to Kafka after this commits — that hop is allowed to fail and retry
    /// independently, because the source of truth already landed safely.
    /// </summary>
    public async Task CompleteAsync(Guid transactionId, Guid customerId, Guid? fromAccountId, Guid? toAccountId, decimal amount, string currency, string type)
    {
        using var conn = Connection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync(
                "UPDATE transactions SET status = 'COMPLETED', completed_at = now() WHERE transaction_id = @transactionId",
                new { transactionId }, tx);

            if (fromAccountId is not null)
                await conn.ExecuteAsync(
                    """
                    INSERT INTO ledger_entries (transaction_id, account_id, entry_type, amount)
                    VALUES (@transactionId, @accountId, 'DEBIT', @amount)
                    """,
                    new { transactionId, accountId = fromAccountId, amount }, tx);

            if (toAccountId is not null)
                await conn.ExecuteAsync(
                    """
                    INSERT INTO ledger_entries (transaction_id, account_id, entry_type, amount)
                    VALUES (@transactionId, @accountId, 'CREDIT', @amount)
                    """,
                    new { transactionId, accountId = toAccountId, amount }, tx);

            var payload = JsonSerializer.Serialize(new
            {
                eventId = Guid.NewGuid(),
                eventType = "TransactionCompleted",
                transactionId,
                fromAccountId,
                toAccountId,
                customerId,
                amount,
                currency,
                type,
                completedAt = DateTime.UtcNow
            });

            await conn.ExecuteAsync(
                """
                INSERT INTO outbox_events (aggregate_id, event_type, payload)
                VALUES (@transactionId, 'TransactionCompleted', @payload::jsonb)
                """,
                new { transactionId, payload }, tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task FailAsync(Guid transactionId, Guid? customerId, decimal amount, string reason)
    {
        using var conn = Connection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync(
                "UPDATE transactions SET status = 'FAILED', failure_reason = @reason WHERE transaction_id = @transactionId",
                new { transactionId, reason }, tx);

            var payload = JsonSerializer.Serialize(new
            {
                eventId = Guid.NewGuid(),
                eventType = "TransactionFailed",
                transactionId,
                customerId,
                amount,
                failureReason = reason,
                failedAt = DateTime.UtcNow
            });

            await conn.ExecuteAsync(
                """
                INSERT INTO outbox_events (aggregate_id, event_type, payload)
                VALUES (@transactionId, 'TransactionFailed', @payload::jsonb)
                """,
                new { transactionId, payload }, tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Transaction>> GetHistoryForAccountAsync(Guid accountId)
    {
        using var conn = Connection();
        return await conn.QueryAsync<Transaction>(
            """
            SELECT transaction_id AS TransactionId, from_account_id AS FromAccountId,
                   to_account_id AS ToAccountId, amount AS Amount, currency AS Currency,
                   type AS Type, status AS Status, idempotency_key AS IdempotencyKey,
                   failure_reason AS FailureReason, created_at AS CreatedAt, completed_at AS CompletedAt
            FROM transactions
            WHERE from_account_id = @accountId OR to_account_id = @accountId
            ORDER BY created_at DESC
            """,
            new { accountId });
    }

    public async Task<IEnumerable<OutboxRow>> GetUnpublishedEventsAsync(int limit = 50)
    {
        using var conn = Connection();
        return await conn.QueryAsync<OutboxRow>(
            """
            SELECT event_id AS EventId, aggregate_id AS AggregateId,
                   event_type AS EventType, payload::text AS Payload
            FROM outbox_events WHERE published = FALSE
            ORDER BY created_at ASC LIMIT @limit
            """,
            new { limit });
    }

    public async Task MarkPublishedAsync(Guid eventId)
    {
        using var conn = Connection();
        await conn.ExecuteAsync(
            "UPDATE outbox_events SET published = TRUE, published_at = now() WHERE event_id = @eventId",
            new { eventId });
    }
}

public class OutboxRow
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
}
