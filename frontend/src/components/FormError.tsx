import { useEffect, useState } from "react";
import { TriangleAlert } from "lucide-react";

/** How long a validation/API error stays on screen before fading out. Pairs with the
 *  `.form-error--reserved` opacity transition in index.css. */
const ERROR_DISMISS_MS = 5000;

/**
 * Error state that clears itself after a delay.
 *
 * Returns a stable setter, so it can be called from event handlers without re-triggering the
 * timer effect on every render. Setting the same message twice in a row still restarts the
 * countdown, since the timer keys off a bumped token rather than the message text.
 */
export function useDismissingError() {
  const [state, setState] = useState<{ message: string | null; token: number }>({
    message: null,
    token: 0,
  });

  useEffect(() => {
    if (!state.message) return;
    const timer = setTimeout(() => setState({ message: null, token: 0 }), ERROR_DISMISS_MS);
    return () => clearTimeout(timer);
  }, [state]);

  function setError(message: string | null) {
    setState((prev) => ({ message, token: prev.token + 1 }));
  }

  return [state.message, setError] as const;
}

interface FormErrorProps {
  message: string | null;
  /**
   * Reserves the message's height even while empty, so showing/hiding it fades in place instead
   * of shifting whatever follows it. Leave off for errors stacked above a form, where a plain
   * mount/unmount is fine.
   */
  reserveSpace?: boolean;
  /** Tucks the reserved box up closer to the preceding control (used inside dense modals). */
  compact?: boolean;
  /** Trims the reserved box's margin on both sides slightly, rather than just above. */
  tight?: boolean;
}

/**
 * The app's validation/API error banner. Two shapes:
 *  - default: renders only when there's a message (used above forms)
 *  - reserveSpace: always mounted, fades opacity (used below a submit button, where a
 *    mount/unmount would visibly push the content underneath it)
 */
export function FormError({ message, reserveSpace, compact, tight }: FormErrorProps) {
  if (!reserveSpace) {
    if (!message) return null;
    return (
      <p className="form-error">
        <TriangleAlert size={16} strokeWidth={2} aria-hidden="true" />
        <span className="form-error-text">{message}</span>
      </p>
    );
  }

  const className = [
    "form-error",
    "form-error--reserved",
    compact ? "form-error--compact" : null,
    tight ? "form-error--tight" : null,
    message ? "form-error--visible" : null,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <p className={className}>
      <TriangleAlert size={16} strokeWidth={2} aria-hidden="true" />
      <span className="form-error-text">{message}</span>
    </p>
  );
}
