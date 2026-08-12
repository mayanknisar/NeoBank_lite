import { maskAccountNumber } from "../utils/format.js";

export default function CustomerSidebar({ accounts, selectedAccountId, onSelectAccount, onLogout }) {
  return (
    <aside className="sidebar">
      <div className="sidebar__top">
        <div>
          <p className="sidebar__brand">NeoBank</p>
          <p className="sidebar__brand-sub">My accounts</p>
        </div>
        <button className="sidebar__logout" onClick={onLogout}>
          Sign out
        </button>
      </div>

      {accounts.length === 0 ? (
        <p className="sidebar__empty">No accounts found for this customer ID.</p>
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
