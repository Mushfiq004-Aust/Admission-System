import { useMemo } from "react";
import { ErrorMessage } from "../components/ErrorMessage";
import { LoadingState } from "../components/LoadingState";
import { Link } from "react-router-dom";
import { StatusBadge } from "../components/StatusBadge";
import { useStudent } from "../context/StudentContext";
import { formatRemaining, useCountdown } from "../hooks/useCountdown";
import { usePolling } from "../hooks/usePolling";
import { api } from "../services/api";
import type { Hold, HoldStatus } from "../types";

function ApplicationRow({ hold }: { hold: Hold }) {
  // serverNow - clientNow, computed once per server response (not on every render).
  const skewMs = useMemo(
    () => (hold.serverTime ? Date.parse(hold.serverTime) - Date.now() : 0),
    [hold.serverTime],
  );
  const remainingMs = useCountdown(hold.status === "HELD" ? hold.expiresAt : undefined, skewMs);
  const status: HoldStatus = hold.status === "HELD" && remainingMs <= 0 ? "EXPIRED" : hold.status;

  let timeCell: string;
  if (status === "HELD") timeCell = `${formatRemaining(remainingMs)} left`;
  else if (status === "CONFIRMED") timeCell = hold.confirmedAt ? `Paid ${new Date(hold.confirmedAt).toLocaleString()}` : "Paid";
  else if (status === "EXPIRED") timeCell = `Expired ${new Date(hold.expiresAt).toLocaleString()}`;
  else timeCell = "Cancelled";

  return (
    <tr>
      <td>{hold.className}</td>
      <td className="mono small">{hold.holdId}</td>
      <td><StatusBadge status={status} /></td>
      <td>{timeCell}</td>
      <td><Link to={`/applications/${hold.holdId}`}>Open</Link></td>
    </tr>
  );
}

export function MyApplications() {
  const { studentId } = useStudent();
  const { data: holds, error, loading } = usePolling(
    () => api.myHolds(studentId!),
    5000,
    `me:${studentId}`,
    studentId !== null,
  );

  if (!studentId) {
    return (
      <section>
        <h1>My applications</h1>
        <p className="alert alert-info">Enter the student's birth registration number at the top of the page to see their applications.</p>
      </section>
    );
  }

  return (
    <section>
      <h1>My applications</h1>
      <p className="lead">All applications for student <span className="mono">{studentId}</span>.</p>

      {error && <ErrorMessage message={error} />}
      {loading && !holds && <LoadingState text="Loading applications…" />}
      {holds && holds.length === 0 && (
        <p className="empty">No applications yet. <Link to="/">Pick a class to apply.</Link></p>
      )}

      {holds && holds.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Class</th><th>Application ID</th><th>Status</th><th>Time</th><th></th></tr>
            </thead>
            <tbody>
              {holds.map((h) => (
                <ApplicationRow key={h.holdId} hold={h} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
