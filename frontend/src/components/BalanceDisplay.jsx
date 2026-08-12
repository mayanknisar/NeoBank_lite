import { useEffect, useRef, useState } from "react";

const formatter = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

const prefersReducedMotion =
  typeof window !== "undefined" &&
  window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;

// Counts up from 0 to `value` once on mount/value-change — reads like a
// ledger balance being tallied rather than a snap-in number.
export default function BalanceDisplay({ value }) {
  const [display, setDisplay] = useState(prefersReducedMotion ? value : 0);
  const frame = useRef();

  useEffect(() => {
    if (prefersReducedMotion) {
      setDisplay(value);
      return;
    }
    const duration = 700;
    const start = performance.now();

    function tick(now) {
      const progress = Math.min((now - start) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setDisplay(value * eased);
      if (progress < 1) frame.current = requestAnimationFrame(tick);
    }

    frame.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame.current);
  }, [value]);

  return <span className="balance__amount">{formatter.format(display)}</span>;
}
