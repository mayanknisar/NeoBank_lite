import { useEffect, useState } from "react";
import CustomerSidebar from "../components/CustomerSidebar.jsx";
import AccountLedger from "../components/AccountLedger.jsx";
import { getCustomerAccounts, getAccount, getBalance, getKycStatus } from "../api/accountApi.js";

export default function CustomerPortal({ customerId, onLogout }) {
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState(null);
  const [account, setAccount] = useState(null);
  const [balance, setBalance] = useState(null);
  const [kyc, setKyc] = useState(null);
  const [error, setError] = useState(null);
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [loadingDetail, setLoadingDetail] = useState(false);

  // Accounts are loaded once for the logged-in customer — there's no
  // customer-switching in this view, unlike the admin dashboard.
  useEffect(() => {
    let cancelled = false;
    setLoadingAccounts(true);
    setError(null);
    getCustomerAccounts(customerId)
      .then((result) => {
        if (cancelled) return;
        setAccounts(result);
        if (result.length > 0) setSelectedAccountId(result[0].accountId);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoadingAccounts(false);
      });
    return () => {
      cancelled = true;
    };
  }, [customerId]);

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

  return (
    <div className="app-shell">
      <CustomerSidebar
        accounts={accounts}
        selectedAccountId={selectedAccountId}
        onSelectAccount={setSelectedAccountId}
        onLogout={onLogout}
      />

      <main className="ledger">
        <p className="ledger__greeting">NeoBank Lite</p>
        <h1 className="ledger__title">My accounts</h1>
        <hr className="ledger__rule" />

        {error && (
          <div className="state-panel state-panel--error">Couldn't load that: {error}</div>
        )}

        {!error && loadingAccounts && <div className="state-panel">Loading your accounts…</div>}

        {!error && !loadingAccounts && accounts.length === 0 && (
          <div className="state-panel">No accounts found for this customer ID.</div>
        )}

        {!error && loadingDetail && (
          <div className="ledger-page">
            <div className="skeleton" style={{ height: 20, width: 140, marginBottom: 16 }} />
            <div className="skeleton" style={{ height: 44, width: 260, marginBottom: 24 }} />
            <div className="skeleton" style={{ height: 14, width: "100%" }} />
          </div>
        )}

        {!error && !loadingDetail && account && balance && (
          <AccountLedger account={account} balance={balance} kyc={kyc} readOnly />
        )}
      </main>
    </div>
  );
}
