const BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5001";

async function request(path) {
  const res = await fetch(`${BASE_URL}${path}`);
  if (!res.ok) {
    let detail = res.statusText;
    try {
      const body = await res.json();
      detail = body?.error?.message || detail;
    } catch {
      /* response wasn't JSON — fall back to statusText */
    }
    throw new Error(detail);
  }
  return res.json();
}

async function post(path, payload) {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  const body = await res.json().catch(() => null);
  if (!res.ok) {
    throw new Error(body?.message || body?.error?.message || res.statusText);
  }
  return body;
}

// GET /api/customers/{customerId}/accounts
export function getCustomerAccounts(customerId) {
  return request(`/api/customers/${customerId}/accounts`);
}

// GET /api/accounts/{accountId}
export function getAccount(accountId) {
  return request(`/api/accounts/${accountId}`);
}

// GET /api/accounts/{accountId}/balance
export function getBalance(accountId) {
  return request(`/api/accounts/${accountId}/balance`);
}

// GET /api/customers/{customerId}/kyc — 404 just means "not submitted yet",
// callers should treat that as a normal empty state, not an error.
export function getKycStatus(customerId) {
  return request(`/api/customers/${customerId}/kyc`);
}

// POST /api/accounts/{accountId}/debit  { amount } -> { success, newBalance, version }
export function debitAccount(accountId, amount) {
  return post(`/api/accounts/${accountId}/debit`, amount);
}

// POST /api/accounts/{accountId}/credit  { amount } -> { success, newBalance, version }
export function creditAccount(accountId, amount) {
  return post(`/api/accounts/${accountId}/credit`, amount);
}

// POST /api/customers  { fullName, email, phone, dateOfBirth } -> { customerId }
export function createCustomer(customer) {
  return post(`/api/customers`, customer);
}

// POST /api/customers/{customerId}/accounts  { accountType, initialDeposit } -> Account
export function createAccount(customerId, payload) {
  return post(`/api/customers/${customerId}/accounts`, payload);
}
