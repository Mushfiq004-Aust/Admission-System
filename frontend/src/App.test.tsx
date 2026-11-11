import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";
import { AdminProvider } from "./context/AdminContext";
import { StudentProvider } from "./context/StudentContext";
import { api } from "./services/api";

function renderApp(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <StudentProvider>
        <AdminProvider>
          <App />
        </AdminProvider>
      </StudentProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  localStorage.clear();
  sessionStorage.clear();
  vi.spyOn(api, "listClasses").mockResolvedValue([
    { id: "c1", name: "Class 6 - Morning Shift", totalSeats: 120, heldSeats: 15, confirmedSeats: 80, availableSeats: 25 },
  ]);
});
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe("two separate portals", () => {
  it("the homepage is the student portal with the class list and no admin link", async () => {
    renderApp("/");

    expect(await screen.findByText("Class 6 - Morning Shift")).toBeTruthy();
    expect(screen.getByRole("link", { name: "My Applications" })).toBeTruthy();
    expect(screen.getByLabelText("Student birth registration number")).toBeTruthy();
    expect(screen.queryByRole("link", { name: /admin/i })).toBeNull();
  });

  it("an unauthenticated visitor to /admin lands on the admin login page (no student box there)", async () => {
    renderApp("/admin");

    expect(await screen.findByRole("heading", { name: "Admin login" })).toBeTruthy();
    expect(screen.queryByLabelText("Student birth registration number")).toBeNull();
  });
});
