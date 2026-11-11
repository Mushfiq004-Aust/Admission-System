import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api } from "../services/api";
import type { AdminSession } from "../types";

// The admin token lives in sessionStorage: it survives a page refresh but disappears when the
// browser tab is closed. The password is never stored anywhere; only the signed, expiring token.
const STORAGE_KEY = "school-admission.adminSession";

interface AdminContextValue {
  session: AdminSession | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AdminContext = createContext<AdminContextValue | null>(null);

function readStored(): AdminSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const s = JSON.parse(raw) as AdminSession;
    if (!s.token || !s.expiresAt || Date.parse(s.expiresAt) <= Date.now()) return null;
    return s;
  } catch {
    return null;
  }
}

export function AdminProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AdminSession | null>(readStored);

  const login = useCallback(async (username: string, password: string) => {
    const s = await api.adminLogin(username, password);
    setSession(s);
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    } catch {
      /* storage unavailable: the admin just has to log in again after a refresh */
    }
  }, []);

  const logout = useCallback(() => {
    setSession(null);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }, []);

  // Log out automatically when the token expires (the backend refuses it from then on anyway).
  useEffect(() => {
    if (!session) return;
    const ms = Date.parse(session.expiresAt) - Date.now();
    if (ms <= 0) {
      logout();
      return;
    }
    const id = window.setTimeout(logout, Math.min(ms, 2_000_000_000));
    return () => window.clearTimeout(id);
  }, [session, logout]);

  const value = useMemo(() => ({ session, login, logout }), [session, login, logout]);
  return <AdminContext.Provider value={value}>{children}</AdminContext.Provider>;
}

export function useAdmin(): AdminContextValue {
  const ctx = useContext(AdminContext);
  if (!ctx) throw new Error("useAdmin must be used inside <AdminProvider>");
  return ctx;
}
