import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AdminProvider } from "../context/AdminContext";
import { ApiError, api } from "../services/api";
import { RequireAdmin } from "./RequireAdmin";

const KEY = "school-admission.adminSession";

function storeSession(expiresInMs: number) {
  sessionStorage.setItem(
    KEY,
    JSON.stringify({ token: "t.t.t", username: "admin", expiresAt: new Date(Date.now() + expiresInMs).toISOString() }),
  );
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AdminProvider>
        <Routes>
          <Route path="/admin/login" element={<div>LOGIN PAGE</div>} />
          <Route path="/admin" element={<RequireAdmin />}>
            <Route index element={<div>DASHBOARD</div>} />
          </Route>
        </Routes>
      </AdminProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => sessionStorage.clear());
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe("RequireAdmin (admin route guard)", () => {
  it("sends visitors without a session to the login page", async () => {
    renderAt("/admin");
    expect(await screen.findByText("LOGIN PAGE")).toBeTruthy();
    expect(screen.queryByText("DASHBOARD")).toBeNull();
  });

  it("shows the admin page when the stored token is accepted by the backend", async () => {
    storeSession(60 * 60 * 1000);
    const me = vi.spyOn(api, "adminMe").mockResolvedValue({ username: "admin" });

    renderAt("/admin");

    expect(await screen.findByText("DASHBOARD")).toBeTruthy();
    expect(me).toHaveBeenCalledWith("t.t.t");
  });

  it("logs out and returns to login when the backend rejects the token", async () => {
    storeSession(60 * 60 * 1000);
    vi.spyOn(api, "adminMe").mockRejectedValue(new ApiError(401, "UNAUTHORIZED", "expired"));

    renderAt("/admin");

    expect(await screen.findByText("LOGIN PAGE")).toBeTruthy();
    await waitFor(() => expect(sessionStorage.getItem(KEY)).toBeNull());
  });

  it("ignores a stored session that has already expired", async () => {
    storeSession(-1000);
    const me = vi.spyOn(api, "adminMe").mockResolvedValue({ username: "admin" });

    renderAt("/admin");

    expect(await screen.findByText("LOGIN PAGE")).toBeTruthy();
    expect(me).not.toHaveBeenCalled();
  });
});
