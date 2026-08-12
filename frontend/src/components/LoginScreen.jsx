import { useState } from "react";

export default function LoginScreen({ onLogin }) {
  const [mode, setMode] = useState(null); // null | "customer"
  const [customerId, setCustomerId] = useState("");

  function handleCustomerSubmit(e) {
    e.preventDefault();
    if (customerId.trim()) onLogin({ role: "customer", customerId: customerId.trim() });
  }

  return (
    <div className="login-screen">
      <div className="login-card">
        <p className="login-card__brand">NeoBank Ledger</p>
        <h1 className="login-card__title">Sign in</h1>
        <p className="login-card__hint">
          Demo login — role determines what you can do here. There's no
          password yet, this just simulates access control on the frontend.
        </p>

        <div className="login-options">
          <button className="login-option" onClick={() => onLogin({ role: "admin" })}>
            <span className="login-option__label">Admin</span>
            <span className="login-option__desc">
              Create customers and accounts, credit or debit any account
            </span>
          </button>

          <button
            className={`login-option${mode === "customer" ? " login-option--active" : ""}`}
            onClick={() => setMode("customer")}
          >
            <span className="login-option__label">Customer</span>
            <span className="login-option__desc">
              View your own accounts and balances
            </span>
          </button>
        </div>

        {mode === "customer" && (
          <form className="login-form" onSubmit={handleCustomerSubmit}>
            <label className="sidebar__label" htmlFor="loginCustomerId" style={{ color: "var(--ink-soft)" }}>
              Customer ID
            </label>
            <div className="sidebar__input-row">
              <input
                id="loginCustomerId"
                className="login-form__input"
                value={customerId}
                onChange={(e) => setCustomerId(e.target.value)}
                placeholder="00000000-0000-..."
                spellCheck={false}
                autoFocus
              />
              <button className="sidebar__go" type="submit">
                Enter
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
