import type { AdmissionClass } from "../types";

export function SeatStatistics({ cls }: { cls: AdmissionClass }) {
  const pct = (n: number) => (cls.totalSeats > 0 ? Math.min(100, (n / cls.totalSeats) * 100) : 0);
  return (
    <div className="seats">
      <div className="seats-available">
        <span className="seats-available-number">{cls.availableSeats}</span>
        <span className="seats-available-label">seats available</span>
      </div>
      <div
        className="seat-bar"
        role="img"
        aria-label={`${cls.confirmedSeats} confirmed, ${cls.heldSeats} held, ${cls.availableSeats} available`}
      >
        <div className="seat-bar-confirmed" style={{ width: `${pct(cls.confirmedSeats)}%` }} />
        <div className="seat-bar-held" style={{ width: `${pct(cls.heldSeats)}%` }} />
      </div>
      <dl className="seat-numbers">
        <div><dt>Total</dt><dd>{cls.totalSeats}</dd></div>
        <div><dt>Held</dt><dd>{cls.heldSeats}</dd></div>
        <div><dt>Confirmed</dt><dd>{cls.confirmedSeats}</dd></div>
        <div><dt>Available</dt><dd>{cls.availableSeats}</dd></div>
      </dl>
    </div>
  );
}
