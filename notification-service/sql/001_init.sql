-- Notification Service DB: notification_db
-- Owns: notifications (pure consumer, no writes back to other services)

CREATE TABLE notifications (
    notification_id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id          UUID NOT NULL,
    channel               VARCHAR(10) NOT NULL CHECK (channel IN ('EMAIL','SMS','PUSH')),
    event_type            VARCHAR(50) NOT NULL,   -- source Kafka event type
    source_event_id       UUID NOT NULL,          -- for idempotent consumption / dedupe
    subject                VARCHAR(150),
    body                   TEXT NOT NULL,
    status                 VARCHAR(20) NOT NULL DEFAULT 'SENT'
                          CHECK (status IN ('SENT','FAILED')),
    created_at             TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Dedup guard: Kafka gives at-least-once delivery, so consumers must be idempotent
CREATE UNIQUE INDEX idx_notifications_dedup ON notifications(source_event_id, channel);
CREATE INDEX idx_notifications_customer ON notifications(customer_id);
