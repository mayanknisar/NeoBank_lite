# NeoBank Lite — Digital Banking Platform (Learning Project)

A BFS-domain microservices project covering sync (gRPC/REST) + async (Kafka)
communication, Redis caching (two different strategies), Postgres
database-per-service, and a React frontend — behind a single API Gateway.

## Architecture

```
                         ┌─────────────┐
                         │   React     │
                         │  Frontend   │
                         └──────┬──────┘
                                │ REST
                         ┌──────▼──────┐
                         │ API Gateway │
                         └──────┬──────┘
        ┌───────────────┬──────┴───────┬───────────────┐
        │ REST           │ REST         │ REST           │
  ┌─────▼─────┐   ┌──────▼──────┐  ┌───▼────┐   ┌────────▼────────┐
  │  Account  │◄──┤ Transaction │  │  Loan   │   │  Notification   │
  │  Service  │gRPC   Service   │  │ Service │   │    Service      │
  │ (+Redis)  │◄──┴──────┬──────┘  └───┬────┘   └────────▲────────┘
  └─────┬─────┘   gRPC   │             │ async            │ async
        │Postgres        │ async       │ Kafka            │ Kafka
        │                ▼ Kafka       ▼                  │
        │          ┌──────────────────────────────────────┘
        │          │        Kafka (transactions, loans, kyc-events)
   ┌────▼───┐ ┌────▼────┐ ┌────────┐
   │account_│ │transact-│ │loan_db │  ...+ notification_db
   │  db    │ │ion_db   │ │        │
   └────────┘ └─────────┘ └────────┘
```

## Folder structure
```
neobank-lite/
├── account-service/
│   ├── sql/001_init.sql          # customers, kyc_status, accounts
│   └── proto/account.proto       # gRPC contract (server)
├── transaction-service/
│   ├── sql/001_init.sql          # transactions, ledger_entries, outbox_events
│   └── proto/                    # (gRPC client stubs generated from account.proto)
├── loan-service/
│   ├── sql/001_init.sql          # loan_applications, interest_rate_slabs, emi_schedule
│   └── proto/                    # (gRPC client stubs generated from account.proto)
├── notification-service/
│   └── sql/001_init.sql          # notifications (pure Kafka consumer)
├── api-gateway/
│   └── rest-contracts.md         # REST surface exposed to React
├── frontend/
│   └── src/                      # React app
├── kafka/
│   └── schemas/                  # transaction-events.json, loan-events.json, kyc-events.json
├── docker-compose.yml
└── README.md
```

## Communication matrix
| From → To | Type | Protocol | Why |
|---|---|---|---|
| React → API Gateway → services | Sync | REST | Standard client-facing API |
| Transaction Service → Account Service | Sync | gRPC | Debit must succeed/fail before responding to the client — strong consistency needed |
| Loan Service → Account Service | Sync | gRPC | Need current account standing before approving |
| Transaction/Loan/Account → Notification Service | Async | Kafka | Notification is not on the critical path — eventual consistency is fine |

## Caching strategy (deliberately two patterns)
- **Account Service**: cache-aside on `balance` + profile. Read: check Redis → miss → read Postgres → populate Redis. Write: update Postgres → invalidate (or update) Redis key. Short TTL since balance changes often.
- **Loan Service**: cache reference data (`interest_rate_slabs`) with a long TTL (~1hr) since it rarely changes — a good contrast to show you understand caching isn't one-size-fits-all.

## Build order
1. Account Service + Postgres + read-only React dashboard
2. Add Redis to Account Service (cache-aside)
3. Transaction Service + gRPC sync call into Account Service
4. Kafka + Notification Service (async consumer loop)
5. Loan Service (reuses Account gRPC client + adds its own Redis cache)
6. API Gateway + full React app (transfer form, loan form, notification toasts)

## Stretch goals
- OpenTelemetry + Jaeger for distributed tracing across the gRPC + Kafka hops
- Idempotency keys already modeled in `transactions.idempotency_key` — enforce end-to-end
- Outbox pattern already modeled in `transaction-service` — wire up a poller (or Debezium CDC) to actually publish to Kafka
- Circuit breaker (Polly, if .NET) around the Transaction → Account gRPC call

## Running locally
```bash
docker compose up --build
# React:        http://localhost:3000
# API Gateway:   http://localhost:8080
# Kafka broker:  localhost:9092
# Postgres:      localhost:5432
# Redis:         localhost:6379
```

Each service's `sql/001_init.sql` is mounted so Postgres seeds all four
schemas on first boot. In production these would be four separate databases;
here they're grouped in one Postgres container to keep the compose file
lightweight for local learning.
