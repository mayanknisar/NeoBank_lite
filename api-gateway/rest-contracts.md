# REST Contracts (Gateway-facing)

React talks only to the API Gateway over REST. The gateway routes to
services; service-to-service calls that need strong consistency use gRPC
(see each service's `proto/` folder).

## Account Service — `/api/accounts`
| Method | Path | Description |
|---|---|---|
| GET | `/api/accounts/{accountId}` | Account details (cache-aside via Redis) |
| GET | `/api/accounts/{accountId}/balance` | Current balance |
| GET | `/api/customers/{customerId}/accounts` | All accounts for a customer |
| POST | `/api/customers/{customerId}/kyc` | Submit KYC docs (mock) |

## Transaction Service — `/api/transactions`
| Method | Path | Description |
|---|---|---|
| POST | `/api/transactions/transfer` | Body: `{fromAccountId, toAccountId, amount, idempotencyKey}` |
| POST | `/api/transactions/deposit` | Body: `{toAccountId, amount, idempotencyKey}` |
| POST | `/api/transactions/withdraw` | Body: `{fromAccountId, amount, idempotencyKey}` |
| GET | `/api/accounts/{accountId}/transactions` | Transaction history (paginated) |

`idempotencyKey` is mandatory on every write — the React client generates a
UUID per form submit so retried requests (flaky network, double-click) don't
double-process. Transaction Service enforces uniqueness on this column.

## Loan Service — `/api/loans`
| Method | Path | Description |
|---|---|---|
| POST | `/api/loans/apply` | Body: `{customerId, accountId, loanType, principalAmount, tenureMonths}` |
| GET | `/api/loans/{loanId}` | Loan status + EMI schedule |
| GET | `/api/customers/{customerId}/loans` | All loans for a customer |
| GET | `/api/loans/rates?loanType=PERSONAL` | Interest rate slabs (Redis-cached, TTL ~1hr) |

## Notification Service — `/api/notifications`
| Method | Path | Description |
|---|---|---|
| GET | `/api/customers/{customerId}/notifications` | Recent notifications (for the toast/bell UI) |
| GET | `/api/notifications/stream?customerId=` | SSE endpoint for real-time toasts |

## Error shape (all services)
```json
{
  "error": {
    "code": "INSUFFICIENT_FUNDS",
    "message": "Account balance is lower than requested debit amount.",
    "traceId": "..."
  }
}
```
Use consistent error codes so the React app can map them to user-facing copy
without string-matching on `message`.
