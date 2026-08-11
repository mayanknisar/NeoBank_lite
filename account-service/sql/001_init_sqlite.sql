PRAGMA foreign_keys = ON;

CREATE TABLE customers (
    customer_id     TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    full_name       TEXT NOT NULL,
    email           TEXT UNIQUE NOT NULL,
    phone           TEXT UNIQUE NOT NULL,
    date_of_birth   TEXT NOT NULL,
    created_at      TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at      TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE kyc_status (
    kyc_id          TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    customer_id     TEXT NOT NULL REFERENCES customers(customer_id),
    status          TEXT NOT NULL DEFAULT 'PENDING'
                    CHECK (status IN ('PENDING','VERIFIED','REJECTED')),
    document_type   TEXT,
    verified_at     TEXT,
    created_at      TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE accounts (
    account_id      TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    customer_id     TEXT NOT NULL REFERENCES customers(customer_id),
    account_number  TEXT UNIQUE NOT NULL,
    account_type    TEXT NOT NULL CHECK (account_type IN ('SAVINGS','CURRENT')),
    balance         NUMERIC NOT NULL DEFAULT 0 CHECK (balance >= 0),
    status          TEXT NOT NULL DEFAULT 'ACTIVE'
                    CHECK (status IN ('ACTIVE','FROZEN','CLOSED')),
    version         INTEGER NOT NULL DEFAULT 0,
    created_at      TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at      TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_accounts_customer_id ON accounts(customer_id);
CREATE INDEX idx_kyc_customer_id ON kyc_status(customer_id);

INSERT INTO customers (full_name, email, phone, date_of_birth)
VALUES ('Test User', 'test.user@example.com', '9999999999', '1995-01-01');
