using Confluent.Kafka;
using TransactionService.Data;

namespace TransactionService.Outbox;

/// <summary>
/// Polls transactions_db.outbox_events for unpublished rows and pushes them
/// to Kafka. This is the second half of the outbox pattern — the repository
/// guarantees the event was recorded atomically with the DB state change;
/// this loop guarantees it eventually reaches Kafka too, retrying on its own
/// schedule if the broker is briefly unreachable. A crash here just means
/// the row stays unpublished until the next tick — nothing is lost.
///
/// For a learning project a 2-second poll is fine. A production system would
/// more likely use logical replication / CDC (e.g. Debezium) instead of
/// polling, to cut latency and avoid hammering the table.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<OutboxPublisher> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public OutboxPublisher(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var brokers = config["Kafka:Brokers"] ?? "localhost:9092";
        _producer = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = brokers }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<TransactionRepository>();
                var events = await repo.GetUnpublishedEventsAsync();

                foreach (var evt in events)
                {
                    // All transaction events go on the "transactions" topic per
                    // kafka/schemas/transaction-events.json — partition key is
                    // the transaction id, so events for the same transaction
                    // stay ordered.
                    await _producer.ProduceAsync("transactions", new Message<string, string>
                    {
                        Key = evt.AggregateId.ToString(),
                        Value = evt.Payload
                    }, stoppingToken);

                    await repo.MarkPublishedAsync(evt.EventId);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publish loop failed, will retry next tick");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public override void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        base.Dispose();
    }
}
