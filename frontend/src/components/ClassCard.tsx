import type { AdmissionClass } from "../types";
import { SeatStatistics } from "./SeatStatistics";

interface Props {
  cls: AdmissionClass;
  busy: boolean;
  disabledReason: string | null;
  error: string | null;
  onApply: (cls: AdmissionClass) => void;
}

export function ClassCard({ cls, busy, disabledReason, error, onApply }: Props) {
  const full = cls.availableSeats <= 0;
  return (
    <article className="card class-card">
      <h2>{cls.name}</h2>
      <SeatStatistics cls={cls} />
      <button
        className="btn btn-primary"
        onClick={() => onApply(cls)}
        disabled={busy || full || disabledReason !== null}
        title={disabledReason ?? undefined}
      >
        {busy ? "Applying…" : full ? "Class full" : "Apply"}
      </button>
      {error && <p className="alert alert-error" role="alert">{error}</p>}
    </article>
  );
}
