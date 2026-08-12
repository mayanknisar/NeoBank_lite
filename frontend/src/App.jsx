import { useState } from "react";
import LoginScreen from "./components/LoginScreen.jsx";
import AdminDashboard from "./views/AdminDashboard.jsx";
import CustomerPortal from "./views/CustomerPortal.jsx";

export default function App() {
  const [session, setSession] = useState(null);

  if (!session) {
    return <LoginScreen onLogin={setSession} />;
  }

  if (session.role === "admin") {
    return <AdminDashboard onLogout={() => setSession(null)} />;
  }

  return <CustomerPortal customerId={session.customerId} onLogout={() => setSession(null)} />;
}
