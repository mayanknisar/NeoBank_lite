import { useState } from "react";

function maskAccountNumber(number) {
  if (!number || number.length < 4) return number;
  return `•••• ${number.slice(-4)}`;
}

export default function AccountSidebar({
  customerId,
  onSearch,
  accounts,
  selectedAccountId,
  onSelectAccount
}) {
  const [inputValue, setInputValue] = useState(customerId || "");

  function handleSubmit(e) {
    e.preventDefault();
    if (inputValue.trim()) onSearch(inputValue.trim());
  }

  return (
    <aside className="sidebar">
      <p className="sidebar__brand">NeoBank</p>
      <p className="sidebar__brand-sub">Ledger</p>

      <form className="sidebar__search" onSubmit={handleSubmit}>
        <label className="sidebar__label" htmlFor="customerId">
          Customer ID
        </label>
        <div className="sidebar__input-row">
          <input
            id="customerId"
            className="sidebar__input"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            placeholder="00000000-0000-..."
            spellCheck={false}
          />
          <button className="sidebar__go" type="submit">
            Go
          </button>
        </div>
      </form>

      <span className="sidebar__label">Accounts</span>
      {accounts.length === 0 ? (
        <p className="sidebar__empty">
          Enter a customer ID above to load their accounts.
        </p>
      ) : (
        <ul className="sidebar__tabs">
          {accounts.map((acct) => (
            <li key={acct.accountId}>
              <button
                className={`sidebar__tab${
                  acct.accountId === selectedAccountId ? " sidebar__tab--active" : ""
                }`}
                onClick={() => onSelectAccount(acct.accountId)}
              >
                <span className="sidebar__tab-type">{acct.accountType}</span>
                <span className="sidebar__tab-number">
                  {maskAccountNumber(acct.accountNumber)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </aside>
  );
}

export { maskAccountNumber };
