import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ErrorMessage } from "../../components/ErrorMessage";
import { useAdmin } from "../../context/AdminContext";
import { messageOf } from "../../services/api";

export function AdminLogin() {
  const { session, login } = useAdmin();
  const navigate = useNavigate();
  const location = useLocation();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (session) return <Navigate to="/admin" replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !password) {
      setError("Enter both the username and the password.");
      return;
    }
    setError(null);
    setBusy(true);
    try {
      await login(username.trim(), password);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from && from.startsWith("/admin") ? from : "/admin", { replace: true });
    } catch (err) {
      setError(messageOf(err));
      setBusy(false);
    }
  };

  return (
    <section className="narrow login">
      <h1>Admin login</h1>
      <p className="lead">For school staff only. Parents and guardians do not need to log in.</p>
      <form className="card form" onSubmit={submit} noValidate>
        <label htmlFor="admin-username">Username</label>
        <input id="admin-username" autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} />

        <label htmlFor="admin-password">Password</label>
        <input id="admin-password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />

        <button className="btn btn-primary" type="submit" disabled={busy}>{busy ? "Signing in…" : "Log in"}</button>
        {error && <ErrorMessage message={error} />}
      </form>
    </section>
  );
}
