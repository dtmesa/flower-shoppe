import { useCallback, useRef, useState, type ReactNode } from "react";
import { Modal } from "./Modal";

interface ConfirmOptions {
  title: string;
  message: ReactNode;
  /** Label for the affirmative button. Defaults to "Confirm". */
  confirmLabel?: string;
  cancelLabel?: string;
  /** Styles the affirmative button as destructive rather than primary. */
  danger?: boolean;
  /** Centers the title and message text instead of the default left alignment. */
  centered?: boolean;
}

interface PendingConfirm extends ConfirmOptions {
  resolve: (value: boolean) => void;
}

/**
 * Promise-based replacement for `window.confirm`, so confirmation prompts match the rest of the
 * app instead of dropping into an unstyleable browser dialog.
 *
 * Returns `[confirm, dialog]` - render `dialog` somewhere in the component, and `await confirm({...})`
 * wherever you'd previously have called `window.confirm`:
 *
 *     if (!(await confirm({ title: "Delete?", message: "..." }))) return;
 */
export function useConfirm(): [(options: ConfirmOptions) => Promise<boolean>, ReactNode] {
  const [pending, setPending] = useState<PendingConfirm | null>(null);
  // Held in a ref as well so settle() can always reach the current resolver without being
  // re-created (and without a stale closure) when the component re-renders.
  const pendingRef = useRef<PendingConfirm | null>(null);

  const settle = useCallback((value: boolean) => {
    pendingRef.current?.resolve(value);
    pendingRef.current = null;
    setPending(null);
  }, []);

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      const next = { ...options, resolve };
      pendingRef.current = next;
      setPending(next);
    });
  }, []);

  const dialog = pending ? (
    // Dismissing the modal (X, backdrop, Escape) counts as declining, matching window.confirm.
    <Modal title={pending.title} onClose={() => settle(false)} centeredTitle={pending.centered}>
      <p className={`detail-description${pending.centered ? " detail-description--centered" : ""}`}>
        {pending.message}
      </p>
      <div className="cart-checkout-actions cart-checkout-actions--centered">
        <button type="button" className="btn btn-secondary" onClick={() => settle(false)}>
          {pending.cancelLabel ?? "Cancel"}
        </button>
        <button
          type="button"
          className={`btn ${pending.danger ? "btn-danger btn-danger--action" : "btn-primary"}`}
          onClick={() => settle(true)}
        >
          {pending.confirmLabel ?? "Confirm"}
        </button>
      </div>
    </Modal>
  ) : null;

  return [confirm, dialog];
}
