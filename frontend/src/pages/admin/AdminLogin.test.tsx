import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AdminProvider } from "../../context/AdminContext";
import { ApiError, api } from "../../services/api";
import { AdminLogin } from "./AdminLogin";

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={["/admin/login"]}>
      <AdminProvider>
        <Routes>
          <Route path="/admin/login" element={<AdminLogin />} />
          <Route path="/admin" element={<div>DASHBOARD</div>} />
        </Routes>
      </AdminProvider>
    </MemoryRouter>,
  );
}

function fill(username: string, password: string) {
  fireEvent.change(screen.getByLabelText("Username"), { target: { value: username } });
  fireEvent.change(screen.getByLabelText("Password"), { target: { value: password } });
  fireEvent.click(screen.getByRole("button", { name: "Log in" }));
}

beforeEach(() => sessionStorage.clear());
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe("AdminLogin", () => {
  it("asks for both fields before calling the backend", () => {
    const login = vi.spyOn(api, "adminLogin");
    renderLogin();
    fill("", "");
    expect(screen.getByText("Enter both the username and the password.")).toBeTruthy();
    expect(login).not.toHaveBeenCalled();
  });

  it("shows an error for wrong credentials and stays on the login page", async () => {
    vi.spyOn(api, "adminLogin").mockRejectedValue(new ApiError(401, "INVALID_CREDENTIALS", "Wrong username or password."));
    renderLogin();
    fill("admin", "nope");

    expect(await screen.findByText("Wrong username or password.")).toBeTruthy();
    expect(screen.queryByText("DASHBOARD")).toBeNull();
    expect(sessionStorage.length).toBe(0);
  });

  it("opens the dashboard and keeps only the token (not the password) after a successful login", async () => {
    vi.spyOn(api, "adminLogin").mockResolvedValue({
      token: "a.b.c",
      username: "admin",
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    });
    renderLogin();
    fill("admin", "s3cret-password");

    expect(await screen.findByText("DASHBOARD")).toBeTruthy();
    await waitFor(() => expect(sessionStorage.length).toBe(1));
    const stored = sessionStorage.getItem("school-admission.adminSession") ?? "";
    expect(stored).toContain("a.b.c");
    expect(stored).not.toContain("s3cret-password");
  });
});
