import { useState } from "react";
import BalanceDisplay from "./BalanceDisplay.jsx";
import StatusStamp from "./StatusStamp.jsx";
import { maskAccountNumber } from "../utils/format.js";

export default function AccountLedger({ account, balance, kyc, onDebit, onCredit, readOnly = false }) {
  const [amount, setAmount] = useState("");
  const [actionError, setActionError] = useState(null);
  const [pending, setPending] = useState(false);

  async function handleAction(type) {
    const parsed = Number(amount);
    if (!parsed || parsed <= 0) {
      setActionError("Enter an amount greater than zero.");
      return;
    }
    setActionError(null);
    setPending(true);
    try {
      if (type === "debit") await onDebit(parsed);
      else await onCredit(parsed);
      setAmount("");
    } catch (err) {
      setActionError(err.message);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="ledger-page">
      <div className="ledger-page__header">
        <div>
          <p className="ledger-page__account-type">{account.accountType} account</p>
          <p className="ledger-page__account-number">
            {maskAccountNumber(account.accountNumber)}
          </p>
        </div>
        <div className="ledger-page__stamps">
          <StatusStamp status={account.status} />
          {kyc && <StatusStamp status={kyc.status} />}
        </div>
      </div>

      <div className="balance">
        <BalanceDisplay value={balance.balance} />
        <p className="balance__caption">
          Available balance · version {balance.version}
        </p>
      </div>

      <div className="detail-row">
        <span className="detail-row__label">Account ID</span>
        <span className="detail-row__value">{account.accountId}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Customer ID</span>
        <span className="detail-row__value">{account.customerId}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">KYC</span>
        <span className="detail-row__value">{kyc ? kyc.status : "Not submitted"}</span>
      </div>

      {!readOnly && (
        <div className="move-money">
          <span className="sidebar__label" style={{ color: "var(--ink-soft)" }}>
            Move money
          </span>
          <div className="move-money__row">
            <input
              className="move-money__input"
              type="number"
              min="0"
              step="0.01"
              placeholder="Amount"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
            />
            <button
              className="move-money__btn move-money__btn--credit"
              onClick={() => handleAction("credit")}
              disabled={pending}
            >
              Credit
            </button>
            <button
              className="move-money__btn move-money__btn--debit"
              onClick={() => handleAction("debit")}
              disabled={pending}
            >
              Debit
            </button>
          </div>
          {actionError && <p className="move-money__error">{actionError}</p>}
        </div>
      )}
    </div>
  );
}
