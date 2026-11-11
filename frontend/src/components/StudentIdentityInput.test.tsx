import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { StudentProvider } from "../context/StudentContext";
import { StudentIdentityInput } from "./StudentIdentityInput";

const ID = "20150101234567890";

function renderBox() {
  return render(
    <StudentProvider>
      <StudentIdentityInput />
    </StudentProvider>,
  );
}

function submit(value: string) {
  fireEvent.change(screen.getByLabelText("Student birth registration number"), { target: { value } });
  fireEvent.click(screen.getByRole("button", { name: "Use this number" }));
}

beforeEach(() => localStorage.clear());
afterEach(cleanup);

describe("StudentIdentityInput", () => {
  it("rejects a number that is not exactly 17 digits", () => {
    renderBox();
    submit("12345");
    expect(screen.getByRole("alert").textContent).toMatch(/17 digits/);
    expect(localStorage.getItem("school-admission.studentId")).toBeNull();
  });

  it("only accepts digits while typing", () => {
    renderBox();
    fireEvent.change(screen.getByLabelText("Student birth registration number"), { target: { value: "20a1-5" } });
    expect((screen.getByLabelText("Student birth registration number") as HTMLInputElement).value).toBe("2015");
  });

  it("saves a valid number and restores it after a reload", () => {
    const first = renderBox();
    submit(ID);
    expect(screen.getByText(ID)).toBeTruthy();
    expect(localStorage.getItem("school-admission.studentId")).toBe(ID);

    first.unmount(); // like closing the page
    renderBox();     // like opening it again
    expect(screen.getByText(ID)).toBeTruthy();
  });
});
