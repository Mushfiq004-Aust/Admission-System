import { formatRemaining } from "../hooks/useCountdown";

export function CountdownTimer({ remainingMs }: { remainingMs: number }) {
  const low = remainingMs > 0 && remainingMs <= 60_000;
  return (
    <div className={`countdown${low ? " countdown-low" : ""}`} role="timer" aria-live="off">
      <span className="countdown-value">{formatRemaining(remainingMs)}</span>
      <span className="countdown-caption">left to pay before the seat is released</span>
    </div>
  );
}
