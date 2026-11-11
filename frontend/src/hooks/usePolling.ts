import { useCallback, useEffect, useRef, useState } from "react";
import { messageOf } from "../services/api";

/**
 * Calls `fetcher` immediately and then every `intervalMs`. Re-runs from scratch when `resetKey` changes.
 * Keeps the last good data on screen if a refresh fails, and exposes `reload` for manual refresh.
 * Pauses while the browser tab is hidden (no useless requests in the background).
 */
export function usePolling<T>(fetcher: () => Promise<T>, intervalMs: number, resetKey: string, enabled = true) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  const reload = useCallback(async () => {
    try {
      const result = await fetcherRef.current();
      setData(result);
      setError(null);
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!enabled) {
      setLoading(false);
      return;
    }
    let active = true;
    setLoading(true);
    setData(null);
    void reload();
    // Do not poll while the browser tab is hidden; refresh as soon as it is visible again.
    const id = window.setInterval(() => {
      if (active && document.visibilityState === "visible") void reload();
    }, intervalMs);
    const onVisible = () => {
      if (active && document.visibilityState === "visible") void reload();
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => {
      active = false;
      window.clearInterval(id);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [reload, intervalMs, resetKey, enabled]);

  return { data, error, loading, reload };
}
