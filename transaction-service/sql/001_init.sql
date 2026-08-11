-- Transaction Service DB: transaction_db
-- Owns: transactions, ledger_entries, outbox_events

CREATE TABLE transactions (
    transaction_id      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    from_account_id     UUID,               -- nullable for deposits
    to_account_id       UUID,               -- nullable for withdrawals
    amount              NUMERIC(18,2) NOT NULL CHECK (amount > 0),
    currency            CHAR(3) NOT NULL DEFAULT 'INR',
    type                VARCHAR(20) NOT NULL CHECK (type IN ('TRANSFER','DEPOSIT','WITHDRAWAL')),
    status              VARCHAR(20) NOT NULL DEFAULT 'PENDING'
                        CHECK (status IN ('PENDING','COMPLETED','FAILED','REVERSED')),
    idempotency_key     VARCHAR(100) UNIQUE NOT NULL,
    failure_reason      VARCHAR(255),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at        TIMESTAMPTZ
);

-- Double-entry style ledger: every transaction produces 2 balanced rows
CREATE TABLE ledger_entries (
    entry_id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id       UUID NOT NULL REFERENCES transactions(transaction_id),
    account_id           UUID NOT NULL,
    entry_type            VARCHAR(6) NOT NULL CHECK (entry_type IN ('DEBIT','CREDIT')),
    amount                NUMERIC(18,2) NOT NULL CHECK (amount > 0),
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Outbox pattern: write the event in the SAME db transaction as the
-- transaction row, then a background poller/CDC publishes to Kafka.
-- This avoids the dual-write problem (DB commit succeeds, Kafka publish fails).
CREATE TABLE outbox_events (
    event_id        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_id     UUID NOT NULL,          -- transaction_id
    event_type       VARCHAR(50) NOT NULL,   -- e.g. TransactionCompleted
    payload          JSONB NOT NULL,
    published        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    published_at     TIMESTAMPTZ
);

CREATE INDEX idx_txn_from_account ON transactions(from_account_id);
CREATE INDEX idx_txn_to_account ON transactions(to_account_id);
CREATE INDEX idx_ledger_txn_id ON ledger_entries(transaction_id);
CREATE INDEX idx_outbox_unpublished ON outbox_events(published) WHERE published = FALSE;
