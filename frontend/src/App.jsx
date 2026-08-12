import { useEffect, useState } from "react";
import AccountSidebar from "./components/AccountSidebar.jsx";
import AccountLedger from "./components/AccountLedger.jsx";
import {
  getCustomerAccounts,
  getAccount,
  getBalance,
  getKycStatus,
  debitAccount,
  creditAccount
} from "./api/accountApi.js";

export default function App() {
  const [customerId, setCustomerId] = useState("");
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState(null);
  const [account, setAccount] = useState(null);
  const [balance, setBalance] = useState(null);
  const [kyc, setKyc] = useState(null);
  const [error, setError] = useState(null);
  const [loadingAccounts, setLoadingAccounts] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);

  async function handleSearch(id) {
    setCustomerId(id);
    setError(null);
    setAccounts([]);
    setAccount(null);
    setSelectedAccountId(null);
    setLoadingAccounts(true);
    try {
      const result = await getCustomerAccounts(id);
      setAccounts(result);
      if (result.length > 0) setSelectedAccountId(result[0].accountId);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoadingAccounts(false);
    }
  }

  useEffect(() => {
    if (!selectedAccountId) return;
    let cancelled = false;
    setLoadingDetail(true);
    setError(null);
    setKyc(null);

    getAccount(selectedAccountId)
      .then(async (accountRes) => {
        if (cancelled) return;
        setAccount(accountRes);

        // KYC is keyed by customer, not account, so it needs accountRes first.
        // A missing KYC record (404) is a normal "not submitted yet" state,
        // not an error — don't let it block the rest of the page.
        const [balanceResult, kycResult] = await Promise.allSettled([
          getBalance(selectedAccountId),
          getKycStatus(accountRes.customerId)
        ]);

        if (cancelled) return;
        if (balanceResult.status === "fulfilled") {
          setBalance(balanceResult.value);
        } else {
          setError(balanceResult.reason.message);
        }
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
      <AccountSidebar
        customerId={customerId}
        onSearch={handleSearch}
        accounts={accounts}
        selectedAccountId={selectedAccountId}
        onSelectAccount={setSelectedAccountId}
      />

      <main className="ledger">
        <p className="ledger__greeting">NeoBank Lite</p>
        <h1 className="ledger__title">Account overview</h1>
        <hr className="ledger__rule" />

        {error && (
          <div className="state-panel state-panel--error">
            Couldn't load that: {error}
          </div>
        )}

        {!error && loadingAccounts && (
          <div className="state-panel">Loading accounts…</div>
        )}

        {!error && !loadingAccounts && accounts.length === 0 && (
          <div className="state-panel">
            No accounts loaded yet. Enter a customer ID in the sidebar to get
            started.
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
