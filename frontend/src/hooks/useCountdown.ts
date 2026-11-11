import { useEffect, useState } from "react";

/**
 * Milliseconds left until `expiresAt` (an ISO timestamp from the backend).
 * `skewMs` = serverNow - clientNow, so a wrong device clock does not distort the countdown.
 * This is purely visual: the backend decides whether a hold is still valid.
 */
export function useCountdown(expiresAt: string | undefined, skewMs = 0): number {
  const compute = () => (expiresAt ? Math.max(0, Date.parse(expiresAt) - (Date.now() + skewMs)) : 0);
  const [remaining, setRemaining] = useState<number>(compute);

  useEffect(() => {
    setRemaining(compute());
    const id = window.setInterval(() => setRemaining(compute()), 250);
    return () => window.clearInterval(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [expiresAt, skewMs]);

  return remaining;
}

export function formatRemaining(ms: number): string {
  const total = Math.ceil(ms / 1000);
  const m = Math.floor(total / 60);
  const s = total % 60;
  return `${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
}
