import { useEffect, useState } from "react";
import { Check, TriangleAlert } from "lucide-react";

/** How long a message stays on screen before fading out. */
const ERROR_DISMISS_MS = 5000;

/** Keep in step with the `.form-error-slot` transition in components.css. */
const SLIDE_MS = 500;

/**
 * Message state that clears itself after a delay. Named for its first use, but it drives the
 * success banner too - both come and go the same way.
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

interface FormMessageProps {
  message: string | null;
  /**
   * Stronger colouring, for the message that answers a form submission - as opposed to one
   * reporting a smaller action that happens to sit on the same page.
   */
  prominent?: boolean;
}

/**
 * The app's inline message banner, in a failure or a success colouring.
 *
 * It slides open and pushes whatever follows it down, rather than living in a permanently
 * reserved box that reads as a blank gap whenever there is no message. The height is animated
 * with a 0fr/1fr grid row so it settles at whatever the message actually needs, however many
 * lines that takes - there is no fixed height for the text to be trimmed to.
 *
 * The CSS still calls this a "form error" throughout; the class names predate the success
 * variant and are load-bearing for a good deal of per-location spacing, so they were left alone.
 */
function FormMessage({ message, prominent, tone }: FormMessageProps & { tone: "error" | "success" }) {
  // The text outlives `message` by one slide, so the box still has something in it on the way
  // closed - then it goes, rather than lingering in the collapsed box where a screen reader
  // would still find a long-dismissed message.
  const [shownText, setShownText] = useState(message);

  useEffect(() => {
    if (message) {
      setShownText(message);
      return;
    }
    const timer = setTimeout(() => setShownText(null), SLIDE_MS);
    return () => clearTimeout(timer);
  }, [message]);

  const Icon = tone === "success" ? Check : TriangleAlert;

  const className = [
    "form-error-slot",
    message ? "form-error-slot--open" : null,
    prominent ? "form-error-slot--prominent" : null,
    tone === "success" ? "form-error-slot--success" : null,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={className} aria-live="polite">
      <div className="form-error-slot-inner">
        <p className="form-error">
          <Icon size={19} strokeWidth={2} aria-hidden="true" />
          <span className="form-error-text">{shownText}</span>
        </p>
      </div>
    </div>
  );
}

export function FormError(props: FormMessageProps) {
  return <FormMessage {...props} tone="error" />;
}

export function FormSuccess(props: FormMessageProps) {
  return <FormMessage {...props} tone="success" />;
}
