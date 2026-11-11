import { useState, type FormEvent } from "react";
import { isValidStudentId, useStudent } from "../context/StudentContext";

export function StudentIdentityInput() {
  const { studentId, saveStudentId, clearStudentId } = useStudent();
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<string | null>(null);

  const showForm = studentId === null || editing;

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (!isValidStudentId(draft)) {
      setError("Enter exactly 17 digits (the student's birth registration number).");
      return;
    }
    saveStudentId(draft);
    setError(null);
    setEditing(false);
    setDraft("");
  };

  return (
    <div className="identity">
      {showForm ? (
        <form onSubmit={submit} className="identity-form" noValidate>
          <label htmlFor="student-id">Student birth registration number</label>
          <input
            id="student-id"
            inputMode="numeric"
            autoComplete="off"
            maxLength={17}
            placeholder="17 digits, e.g. 20152691234567890"
            value={draft}
            onChange={(e) => setDraft(e.target.value.replace(/\D/g, ""))}
            aria-invalid={error !== null}
            aria-describedby={error ? "student-id-error" : undefined}
          />
          <button className="btn btn-primary" type="submit">Use this number</button>
          {studentId !== null && (
            <button className="btn btn-ghost" type="button" onClick={() => { setEditing(false); setError(null); }}>
              Cancel
            </button>
          )}
          {error && <p id="student-id-error" className="field-error" role="alert">{error}</p>}
        </form>
      ) : (
        <div className="identity-current">
          <span>Applying as student</span>
          <strong className="mono">{studentId}</strong>
          <button className="btn btn-ghost" onClick={() => { setDraft(studentId ?? ""); setEditing(true); }}>Change</button>
          <button className="btn btn-ghost" onClick={clearStudentId}>Sign out</button>
        </div>
      )}
    </div>
  );
}
