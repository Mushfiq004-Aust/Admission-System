import type { AdmissionClass, AdminSession, ConfirmResult, CreatedHold, Hold } from "../types";

declare global {
  interface Window {
    __APP_CONFIG__?: { API_BASE_URL?: string };
  }
}

// Where the backend lives. Order of precedence:
//   1. public/config.js  (in Docker it is rewritten at container start from the VITE_API_BASE_URL env var)
//   2. VITE_API_BASE_URL in frontend/.env (see .env.example) when you run `npm run dev`
//   3. http://localhost:8080
// NOTE: this URL is used by the BROWSER, so it must be reachable from your computer
// (http://localhost:8080), never a Docker-only name like http://backend:8080.
const API_BASE: string = (
  window.__APP_CONFIG__?.API_BASE_URL ||
  import.meta.env.VITE_API_BASE_URL ||
  "http://localhost:8080"
).replace(/\/$/, "");

export class ApiError extends Error {
  status: number;
  code: string;
  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

/** Plain-language messages for the error codes the backend returns. */
const FRIENDLY: Record<string, string> = {
  CLASS_FULL: "Sorry, the last seat was just taken. No seats are available in this class.",
  ALREADY_APPLIED: "This student already has an active application for this class.",
  HOLD_EXPIRED: "The reservation time has run out, so the seat was released. Please apply again.",
  HOLD_CANCELLED: "This application was cancelled.",
  CANNOT_CANCEL_CONFIRMED: "A confirmed admission cannot be cancelled online. Please contact the school office.",
  NOT_OWNER: "This application belongs to a different student.",
  HOLD_NOT_FOUND: "Application not found.",
  CLASS_NOT_FOUND: "Class not found.",
  INVALID_CREDENTIALS: "Wrong username or password.",
  UNAUTHORIZED: "Your admin session is missing or has expired. Please log in again.",
  FORBIDDEN: "You are not allowed to do this.",
  ADMIN_NOT_CONFIGURED: "Admin login is not set up on the server yet (see the README: ADMIN_USERNAME / ADMIN_PASSWORD / JWT_SECRET).",
};

export function messageOf(e: unknown): string {
  if (e instanceof ApiError) return e.message;
  if (e instanceof Error) return e.message;
  return "Something went wrong. Please try again.";
}

async function request<T>(
  path: string,
  opts: { method?: string; body?: unknown; studentId?: string | null; token?: string | null } = {},
): Promise<T> {
  const headers: Record<string, string> = {};
  if (opts.body !== undefined) headers["Content-Type"] = "application/json";
  if (opts.studentId) headers["X-Student-Id"] = opts.studentId;
  if (opts.token) headers["Authorization"] = `Bearer ${opts.token}`; // admin requests only

  let res: Response;
  try {
    res = await fetch(`${API_BASE}${path}`, {
      method: opts.method ?? "GET",
      headers,
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
  } catch {
    throw new ApiError(0, "NETWORK", "Cannot reach the server. Check that the backend is running and try again.");
  }

  if (res.status === 204) return undefined as T;

  let data: unknown = null;
  const text = await res.text();
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = null;
    }
  }

  if (!res.ok) {
    const body = data as { error?: string; message?: string } | null;
    const code = body?.error ?? `HTTP_${res.status}`;
    const message = FRIENDLY[code] ?? body?.message ?? `Request failed (HTTP ${res.status}).`;
    throw new ApiError(res.status, code, message);
  }
  return data as T;
}

export const api = {
  listClasses: () => request<AdmissionClass[]>("/classes"),
  getClass: (classId: string) => request<AdmissionClass>(`/classes/${classId}`),
  // Admin only: needs the token from adminLogin (the backend rejects it otherwise).
  createClass: (name: string, totalSeats: number, token: string) =>
    request<AdmissionClass>("/classes", { method: "POST", body: { name, totalSeats }, token }),
  adminLogin: (username: string, password: string) =>
    request<AdminSession>("/admin/login", { method: "POST", body: { username, password } }),
  adminMe: (token: string) => request<{ username: string }>("/admin/me", { token }),
  adminApplications: (token: string) => request<Hold[]>("/admin/applications", { token }),
  apply: (classId: string, studentId: string) =>
    request<CreatedHold>(`/classes/${classId}/holds`, { method: "POST", studentId }),
  getHold: (holdId: string, studentId: string) => request<Hold>(`/holds/${holdId}`, { studentId }),
  confirmHold: (holdId: string, studentId: string) =>
    request<ConfirmResult>(`/holds/${holdId}/confirm`, { method: "POST", studentId }),
  cancelHold: (holdId: string, studentId: string) =>
    request<void>(`/holds/${holdId}`, { method: "DELETE", studentId }),
  myHolds: (studentId: string) => request<Hold[]>("/me/holds", { studentId }),
};
