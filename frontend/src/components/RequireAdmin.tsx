import { useEffect, useState } from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAdmin } from "../context/AdminContext";
import { ApiError, api } from "../services/api";
import { LoadingState } from "./LoadingState";

// Guard for every admin page: no session -> login page. This is only for convenience;
// the REAL protection is the backend, which checks the token on every admin request.
export function RequireAdmin() {
  const { session, logout } = useAdmin();
  const location = useLocation();
  const token = session?.token ?? null;
  const [checked, setChecked] = useState(false);

  // Ask the backend once whether the stored token is still accepted (it may have expired or been rejected).
  useEffect(() => {
    if (!token) return;
    let alive = true;
    setChecked(false);
    api
      .adminMe(token)
      .then(() => alive && setChecked(true))
      .catch((e) => {
        if (!alive) return;
        if (e instanceof ApiError && (e.status === 401 || e.status === 403)) logout();
        else setChecked(true); // backend unreachable: let the page show its own error messages
      });
    return () => {
      alive = false;
    };
  }, [token, logout]);

  if (!token) return <Navigate to="/admin/login" replace state={{ from: location.pathname }} />;
  if (!checked) return <LoadingState text="Checking your admin session…" />;
  return <Outlet />;
}
