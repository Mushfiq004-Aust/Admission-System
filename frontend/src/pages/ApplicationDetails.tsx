import { useEffect, useMemo, useState } from "react";
import { ErrorMessage } from "../components/ErrorMessage";
import { LoadingState } from "../components/LoadingState";
import { useParams } from "react-router-dom";
import { CountdownTimer } from "../components/CountdownTimer";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { StatusBadge } from "../components/StatusBadge";
import { useStudent } from "../context/StudentContext";
import { useCountdown } from "../hooks/useCountdown";
import { usePolling } from "../hooks/usePolling";
import { api, messageOf } from "../services/api";
import type { HoldStatus } from "../types";

export function ApplicationDetails() {
  const { holdId = "" } = useParams();
  const { studentId } = useStudent();
  const [busy, setBusy] = useState<"pay" | "cancel" | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [confirmCancelOpen, setConfirmCancelOpen] = useState(false);

  const { data: hold, error, loading, reload } = usePolling(
    () => api.getHold(holdId, studentId!),
    3000,
    `${holdId}:${studentId}`,
    studentId !== null,
  );

  // Correct for a wrong device clock using the server's clock from the response.
  const skewMs = useMemo(() => (hold?.serverTime ? Date.parse(hold.serverTime) - Date.now() : 0), [hold?.serverTime]);
  const remainingMs = useCountdown(hold?.expiresAt, skewMs);

  // Show EXPIRED the moment the timer hits zero, and ask the backend for the real status.
  const timedOut = hold?.status === "HELD" && remainingMs <= 0;
  const status: HoldStatus | undefined = timedOut ? "EXPIRED" : hold?.status;
  useEffect(() => {
    if (timedOut) void reload();
  }, [timedOut, reload]);

  const pay = async () => {
    if (!hold || !studentId) return;
    setBusy("pay");
    setActionError(null);
    setNotice(null);
    try {
      await api.confirmHold(hold.holdId, studentId); // simulated payment -> POST /holds/{id}/confirm
      setNotice("Payment received. The admission is confirmed.");
    } catch (e) {
      setActionError(messageOf(e));
    } finally {
      await reload(); // always show what the backend says now
      setBusy(null);
    }
  };

  const cancel = async () => {
    if (!hold || !studentId) return;
    setConfirmCancelOpen(false);
    setBusy("cancel");
    setActionError(null);
    setNotice(null);
    try {
      await api.cancelHold(hold.holdId, studentId);
      setNotice("Application cancelled. The seat was released.");
    } catch (e) {
      setActionError(messageOf(e));
    } finally {
      await reload();
      setBusy(null);
    }
  };

  if (!studentId) {
    return (
      <section className="narrow">
        <h1>Application</h1>
        <p className="alert alert-info">Enter the student's birth registration number at the top of the page to see this application.</p>
      </section>
    );
  }

  return (
    <section className="narrow">
      <h1>Application</h1>
      {loading && !hold && <LoadingState text="Loading application…" />}
      {error && <ErrorMessage message={error} />}

      {hold && status && (
        <div className="card">
          <div className="card-head">
            <h2>{hold.className}</h2>
            <StatusBadge status={status} />
          </div>

          {status === "HELD" && <CountdownTimer remainingMs={remainingMs} />}
          {status === "CONFIRMED" && <p className="alert alert-success">Admission confirmed. The seat is yours.</p>}
          {status === "EXPIRED" && <p className="alert alert-error">This reservation expired and the seat was released. You can apply again from the class list.</p>}
          {status === "CANCELLED" && <p className="alert alert-info">This application was cancelled.</p>}

          <dl className="details">
            <div><dt>Student ID</dt><dd className="mono">{hold.studentId}</dd></div>
            <div><dt>Class</dt><dd>{hold.className}</dd></div>
            <div><dt>Application ID</dt><dd className="mono">{hold.holdId}</dd></div>
            <div><dt>Status</dt><dd>{status}</dd></div>
            <div><dt>Reserved until</dt><dd>{new Date(hold.expiresAt).toLocaleString()}</dd></div>
          </dl>

          <div className="actions">
            <button className="btn btn-primary" onClick={pay} disabled={status !== "HELD" || busy !== null}>
              {busy === "pay" ? "Processing payment…" : "Pay Admission Fee"}
            </button>
            <button className="btn btn-danger" onClick={() => setConfirmCancelOpen(true)} disabled={status !== "HELD" || busy !== null}>
              {busy === "cancel" ? "Cancelling…" : "Cancel"}
            </button>
          </div>

          {notice && <p className="alert alert-success" role="status">{notice}</p>}
          {actionError && <p className="alert alert-error" role="alert">{actionError}</p>}
        </div>
      )}

      <ConfirmDialog
        open={confirmCancelOpen}
        title="Cancel this application?"
        message="The seat will be released and another student can take it."
        confirmLabel="Yes, cancel it"
        cancelLabel="Keep my seat"
        onConfirm={cancel}
        onCancel={() => setConfirmCancelOpen(false)}
      />

    </section>
  );
}
