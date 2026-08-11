-- Account Service DB: account_db
-- Owns: customers, accounts, kyc_status

CREATE TABLE customers (
    customer_id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name       VARCHAR(150) NOT NULL,
    email           VARCHAR(150) UNIQUE NOT NULL,
    phone           VARCHAR(20) UNIQUE NOT NULL,
    date_of_birth   DATE NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE kyc_status (
    kyc_id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID NOT NULL REFERENCES customers(customer_id),
    status          VARCHAR(20) NOT NULL DEFAULT 'PENDING'
                    CHECK (status IN ('PENDING','VERIFIED','REJECTED')),
    document_type   VARCHAR(30),
    verified_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE accounts (
    account_id      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID NOT NULL REFERENCES customers(customer_id),
    account_number  VARCHAR(20) UNIQUE NOT NULL,
    account_type    VARCHAR(20) NOT NULL CHECK (account_type IN ('SAVINGS','CURRENT')),
    balance         NUMERIC(18,2) NOT NULL DEFAULT 0 CHECK (balance >= 0),
    status          VARCHAR(20) NOT NULL DEFAULT 'ACTIVE'
                    CHECK (status IN ('ACTIVE','FROZEN','CLOSED')),
    -- optimistic locking: gRPC debit calls must check this to avoid lost updates
    version         BIGINT NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_accounts_customer_id ON accounts(customer_id);
CREATE INDEX idx_kyc_customer_id ON kyc_status(customer_id);

-- Seed a couple of test rows
INSERT INTO customers (full_name, email, phone, date_of_birth)
VALUES ('Test User', 'test.user@example.com', '9999999999', '1995-01-01');
