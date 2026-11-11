import { useState, type FormEvent } from "react";
import { ApiError, api, messageOf } from "../services/api";
import { ErrorMessage } from "./ErrorMessage";

interface Props {
  token: string;
  onCreated: () => void;
  onUnauthorized: () => void;
}

// Admin form: create one class (each shift is its own class with its own seats).
export function ClassCreateForm({ token, onCreated, onUnauthorized }: Props) {
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setCreated(null);
    const trimmed = name.trim();
    const total = Number(seats);
    if (!trimmed) return setError("Enter a class name, for example “Class 6 - Morning Shift”.");
    if (trimmed.length > 200) return setError("The class name can have at most 200 characters.");
    if (!Number.isInteger(total) || total < 1 || total > 100000) {
      return setError("Total seats must be a whole number between 1 and 100000.");
    }
    setError(null);
    setBusy(true);
    try {
      const c = await api.createClass(trimmed, total, token);
      setCreated(`Created “${c.name}” with ${c.totalSeats} seats.`);
      setName("");
      setSeats("");
      onCreated();
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) onUnauthorized();
      setError(messageOf(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <form className="card form" onSubmit={submit} noValidate>
      <h2>Create a class</h2>
      <p className="muted">Create each shift separately, e.g. “Class 6 - Morning Shift” and “Class 6 - Day Shift”.</p>

      <label htmlFor="class-name">Class name</label>
      <input id="class-name" value={name} maxLength={200} onChange={(e) => setName(e.target.value)} placeholder="Class 6 - Morning Shift" />

      <label htmlFor="class-seats">Total seats</label>
      <input id="class-seats" inputMode="numeric" value={seats} onChange={(e) => setSeats(e.target.value.replace(/\D/g, ""))} placeholder="120" />

      <button className="btn btn-primary" type="submit" disabled={busy}>{busy ? "Creating…" : "Create class"}</button>

      {error && <ErrorMessage message={error} />}
      {created && <p className="alert alert-success" role="status">{created}</p>}
    </form>
  );
}
