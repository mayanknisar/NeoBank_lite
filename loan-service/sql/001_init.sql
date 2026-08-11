-- Loan Service DB: loan_db
-- Owns: loan_applications, interest_rate_slabs, emi_schedule

CREATE TABLE interest_rate_slabs (
    slab_id         SERIAL PRIMARY KEY,
    loan_type       VARCHAR(30) NOT NULL,   -- PERSONAL, HOME, AUTO
    min_amount      NUMERIC(18,2) NOT NULL,
    max_amount      NUMERIC(18,2) NOT NULL,
    interest_rate   NUMERIC(5,2) NOT NULL,  -- annual %, rarely changes -> good Redis candidate
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE loan_applications (
    loan_id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id         UUID NOT NULL,
    account_id          UUID NOT NULL,       -- disbursement target
    loan_type           VARCHAR(30) NOT NULL,
    principal_amount    NUMERIC(18,2) NOT NULL CHECK (principal_amount > 0),
    tenure_months       INT NOT NULL CHECK (tenure_months > 0),
    interest_rate       NUMERIC(5,2) NOT NULL,
    emi_amount          NUMERIC(18,2),
    status              VARCHAR(20) NOT NULL DEFAULT 'APPLIED'
                        CHECK (status IN ('APPLIED','UNDER_REVIEW','APPROVED','REJECTED','DISBURSED')),
    applied_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    decided_at          TIMESTAMPTZ
);

CREATE TABLE emi_schedule (
    schedule_id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id         UUID NOT NULL REFERENCES loan_applications(loan_id),
    installment_no  INT NOT NULL,
    due_date        DATE NOT NULL,
    amount          NUMERIC(18,2) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING','PAID','OVERDUE'))
);

CREATE INDEX idx_loan_customer_id ON loan_applications(customer_id);
CREATE INDEX idx_emi_loan_id ON emi_schedule(loan_id);

INSERT INTO interest_rate_slabs (loan_type, min_amount, max_amount, interest_rate) VALUES
('PERSONAL', 10000, 500000, 12.5),
('HOME', 500000, 10000000, 8.25),
('AUTO', 100000, 2000000, 9.75);
