import { useEffect, useState } from "react";
import AdminSidebar from "../components/AdminSidebar.jsx";
import AccountLedger from "../components/AccountLedger.jsx";
import {
  getCustomerAccounts,
  getAccount,
  getBalance,
  getKycStatus,
  debitAccount,
  creditAccount,
  createCustomer,
  createAccount
} from "../api/accountApi.js";

export default function AdminDashboard({ onLogout }) {
  const [customerId, setCustomerId] = useState(null);
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState(null);
  const [account, setAccount] = useState(null);
  const [balance, setBalance] = useState(null);
  const [kyc, setKyc] = useState(null);
  const [error, setError] = useState(null);
  const [loadingAccounts, setLoadingAccounts] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);

  async function loadAccounts(id) {
    setError(null);
    setLoadingAccounts(true);
    try {
      const result = await getCustomerAccounts(id);
      setAccounts(result);
      setSelectedAccountId(result.length > 0 ? result[0].accountId : null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoadingAccounts(false);
    }
  }

  function handleSearchCustomer(id) {
    setCustomerId(id);
    loadAccounts(id);
  }

  async function handleCreateCustomer(payload) {
    const created = await createCustomer(payload);
    setCustomerId(created.customerId);
    await loadAccounts(created.customerId);
    return created;
  }

  async function handleCreateAccount(payload) {
    await createAccount(customerId, payload);
    await loadAccounts(customerId);
  }

  useEffect(() => {
    if (!selectedAccountId) {
      setAccount(null);
      setBalance(null);
      setKyc(null);
      return;
    }
    let cancelled = false;
    setLoadingDetail(true);
    setError(null);
    setKyc(null);

    getAccount(selectedAccountId)
      .then(async (accountRes) => {
        if (cancelled) return;
        setAccount(accountRes);
        const [balanceResult, kycResult] = await Promise.allSettled([
          getBalance(selectedAccountId),
          getKycStatus(accountRes.customerId)
        ]);
        if (cancelled) return;
        if (balanceResult.status === "fulfilled") setBalance(balanceResult.value);
        else setError(balanceResult.reason.message);
        if (kycResult.status === "fulfilled") setKyc(kycResult.value);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoadingDetail(false);
      });

    return () => {
      cancelled = true;
    };
  }, [selectedAccountId]);

  async function handleDebit(amount) {
    const result = await debitAccount(selectedAccountId, amount);
    setBalance({ accountId: selectedAccountId, balance: result.newBalance, version: result.version });
  }

  async function handleCredit(amount) {
    const result = await creditAccount(selectedAccountId, amount);
    setBalance({ accountId: selectedAccountId, balance: result.newBalance, version: result.version });
  }

  return (
    <div className="app-shell">
      <AdminSidebar
        customerId={customerId}
        onSearchCustomer={handleSearchCustomer}
        accounts={accounts}
        selectedAccountId={selectedAccountId}
        onSelectAccount={setSelectedAccountId}
        onCreateCustomer={handleCreateCustomer}
        onCreateAccount={handleCreateAccount}
        onLogout={onLogout}
      />

      <main className="ledger">
        <p className="ledger__greeting">NeoBank Lite · Admin</p>
        <h1 className="ledger__title">Account overview</h1>
        <hr className="ledger__rule" />

        {error && (
          <div className="state-panel state-panel--error">Couldn't load that: {error}</div>
        )}

        {!error && loadingAccounts && <div className="state-panel">Loading accounts…</div>}

        {!error && !loadingAccounts && !customerId && (
          <div className="state-panel">
            Search for a customer ID, or create a new customer, to get started.
          </div>
        )}

        {!error && !loadingAccounts && customerId && accounts.length === 0 && (
          <div className="state-panel">
            This customer has no accounts yet. Use "+ New account" in the sidebar.
          </div>
        )}

        {!error && loadingDetail && (
          <div className="ledger-page">
            <div className="skeleton" style={{ height: 20, width: 140, marginBottom: 16 }} />
            <div className="skeleton" style={{ height: 44, width: 260, marginBottom: 24 }} />
            <div className="skeleton" style={{ height: 14, width: "100%" }} />
          </div>
        )}

        {!error && !loadingDetail && account && balance && (
          <AccountLedger
            account={account}
            balance={balance}
            kyc={kyc}
            onDebit={handleDebit}
            onCredit={handleCredit}
          />
        )}
      </main>
    </div>
  );
}
