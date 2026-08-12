import { useState } from "react";
import { maskAccountNumber } from "../utils/format.js";

export default function AdminSidebar({
  customerId,
  onSearchCustomer,
  accounts,
  selectedAccountId,
  onSelectAccount,
  onCreateCustomer,
  onCreateAccount,
  onLogout
}) {
  const [searchValue, setSearchValue] = useState(customerId || "");

  const [showNewCustomer, setShowNewCustomer] = useState(false);
  const [newCustomer, setNewCustomer] = useState({
    fullName: "",
    email: "",
    phone: "",
    dateOfBirth: ""
  });
  const [customerError, setCustomerError] = useState(null);
  const [customerPending, setCustomerPending] = useState(false);

  const [showNewAccount, setShowNewAccount] = useState(false);
  const [newAccount, setNewAccount] = useState({ accountType: "SAVINGS", initialDeposit: "" });
  const [accountError, setAccountError] = useState(null);
  const [accountPending, setAccountPending] = useState(false);

  function handleSearchSubmit(e) {
    e.preventDefault();
    if (searchValue.trim()) onSearchCustomer(searchValue.trim());
  }

  async function handleCreateCustomer(e) {
    e.preventDefault();
    setCustomerError(null);
    setCustomerPending(true);
    try {
      const created = await onCreateCustomer(newCustomer);
      setSearchValue(created.customerId);
      setNewCustomer({ fullName: "", email: "", phone: "", dateOfBirth: "" });
      setShowNewCustomer(false);
    } catch (err) {
      setCustomerError(err.message);
    } finally {
      setCustomerPending(false);
    }
  }

  async function handleCreateAccount(e) {
    e.preventDefault();
    setAccountError(null);
    setAccountPending(true);
    try {
      await onCreateAccount({
        accountType: newAccount.accountType,
        initialDeposit: Number(newAccount.initialDeposit) || 0
      });
      setNewAccount({ accountType: "SAVINGS", initialDeposit: "" });
      setShowNewAccount(false);
    } catch (err) {
      setAccountError(err.message);
    } finally {
      setAccountPending(false);
    }
  }

  return (
    <aside className="sidebar">
      <div className="sidebar__top">
        <div>
          <p className="sidebar__brand">NeoBank</p>
          <p className="sidebar__brand-sub">Admin console</p>
        </div>
        <button className="sidebar__logout" onClick={onLogout}>
          Sign out
        </button>
      </div>

      <form className="sidebar__search" onSubmit={handleSearchSubmit}>
        <label className="sidebar__label" htmlFor="customerSearch">
          Customer ID
        </label>
        <div className="sidebar__input-row">
          <input
            id="customerSearch"
            className="sidebar__input"
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            placeholder="00000000-0000-..."
            spellCheck={false}
          />
          <button className="sidebar__go" type="submit">
            Go
          </button>
        </div>
      </form>

      <button className="sidebar__toggle" onClick={() => setShowNewCustomer((v) => !v)}>
        {showNewCustomer ? "Cancel" : "+ New customer"}
      </button>

      {showNewCustomer && (
        <form className="sidebar__form" onSubmit={handleCreateCustomer}>
          <input
            className="sidebar__input"
            placeholder="Full name"
            required
            value={newCustomer.fullName}
            onChange={(e) => setNewCustomer({ ...newCustomer, fullName: e.target.value })}
          />
          <input
            className="sidebar__input"
            placeholder="Email"
            type="email"
            required
            value={newCustomer.email}
            onChange={(e) => setNewCustomer({ ...newCustomer, email: e.target.value })}
          />
          <input
            className="sidebar__input"
            placeholder="Phone"
            required
            value={newCustomer.phone}
            onChange={(e) => setNewCustomer({ ...newCustomer, phone: e.target.value })}
          />
          <input
            className="sidebar__input"
            type="date"
            required
            value={newCustomer.dateOfBirth}
            onChange={(e) => setNewCustomer({ ...newCustomer, dateOfBirth: e.target.value })}
          />
          <button className="sidebar__go sidebar__go--full" type="submit" disabled={customerPending}>
            {customerPending ? "Creating…" : "Create customer"}
          </button>
          {customerError && <p className="move-money__error">{customerError}</p>}
        </form>
      )}

      <span className="sidebar__label">Accounts</span>
      {accounts.length === 0 ? (
        <p className="sidebar__empty">Search for or create a customer to see accounts.</p>
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

      {customerId && (
        <>
          <button className="sidebar__toggle" onClick={() => setShowNewAccount((v) => !v)}>
            {showNewAccount ? "Cancel" : "+ New account"}
          </button>
          {showNewAccount && (
            <form className="sidebar__form" onSubmit={handleCreateAccount}>
              <select
                className="sidebar__input"
                value={newAccount.accountType}
                onChange={(e) => setNewAccount({ ...newAccount, accountType: e.target.value })}
              >
                <option value="SAVINGS">Savings</option>
                <option value="CURRENT">Current</option>
              </select>
              <input
                className="sidebar__input"
                type="number"
                min="0"
                step="0.01"
                placeholder="Initial deposit (optional)"
                value={newAccount.initialDeposit}
                onChange={(e) => setNewAccount({ ...newAccount, initialDeposit: e.target.value })}
              />
              <button className="sidebar__go sidebar__go--full" type="submit" disabled={accountPending}>
                {accountPending ? "Creating…" : "Create account"}
              </button>
              {accountError && <p className="move-money__error">{accountError}</p>}
            </form>
          )}
        </>
      )}
    </aside>
  );
}
