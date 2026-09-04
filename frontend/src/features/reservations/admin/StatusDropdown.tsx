import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { ChevronDown } from "lucide-react";
import type { ReservationStatus } from "../types";

const STATUS_OPTIONS: ReservationStatus[] = ["NEW", "CONTACTED", "CONFIRMED", "COMPLETED", "CANCELLED"];

function formatStatusLabel(status: ReservationStatus): string {
  return status.charAt(0) + status.slice(1).toLowerCase();
}

interface MenuPosition {
  top: number;
  left: number;
  minWidth: number;
}

interface StatusDropdownProps {
  value: ReservationStatus;
  onChange: (status: ReservationStatus) => void;
}

// Bespoke replacement for a native <select> - the browser/OS renders a native select's open
// listbox itself, so it ignores border-radius (and most other styling) no matter what CSS is
// applied to the <select>. This is a real popup we own, so it can match the app's rounded,
// card-like look.
//
// The trigger lives inside the pickup-requests table, which clips overflow for its own rounded
// corners and horizontal scrollbar (see .table-wrapper/.table-scroll) - a plain absolutely
// positioned menu would get cut off by that whenever the table is short. Portaling the menu to
// <body> and positioning it with fixed coordinates from the trigger's own bounding box sidesteps
// that clipping entirely.
export function StatusDropdown({ value, onChange }: StatusDropdownProps) {
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<MenuPosition | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLUListElement>(null);

  useEffect(() => {
    if (!open) return;

    function updatePosition() {
      const rect = triggerRef.current?.getBoundingClientRect();
      if (!rect) return;
      setPosition({ top: rect.bottom + 6, left: rect.left, minWidth: rect.width });
    }

    updatePosition();

    function handlePointerDown(event: MouseEvent) {
      const target = event.target as Node;
      if (triggerRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      setOpen(false);
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    // capture: true so this also fires for scrolling inside .table-scroll (or any other
    // scrollable ancestor), not just the window itself.
    window.addEventListener("scroll", updatePosition, true);
    window.addEventListener("resize", updatePosition);
    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("scroll", updatePosition, true);
      window.removeEventListener("resize", updatePosition);
    };
  }, [open]);

  function handleSelect(status: ReservationStatus) {
    setOpen(false);
    if (status !== value) onChange(status);
  }

  return (
    <div className="status-dropdown">
      <button
        ref={triggerRef}
        type="button"
        className={`status-dropdown-trigger status-select--${value.toLowerCase()}`}
        onClick={() => setOpen((isOpen) => !isOpen)}
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        {formatStatusLabel(value)}
        <ChevronDown size={14} strokeWidth={2.5} aria-hidden="true" className="status-dropdown-chevron" />
      </button>
      {open && position &&
        createPortal(
          <ul
            ref={menuRef}
            className="status-dropdown-menu"
            role="listbox"
            style={{ top: position.top, left: position.left, minWidth: position.minWidth }}
          >
            {STATUS_OPTIONS.map((status) => (
              <li key={status} role="option" aria-selected={status === value}>
                <button
                  type="button"
                  className={`status-dropdown-option status-select--${status.toLowerCase()}${
                    status === value ? " status-dropdown-option--active" : ""
                  }`}
                  onClick={() => handleSelect(status)}
                >
                  <span className="status-dropdown-option-label">{formatStatusLabel(status)}</span>
                </button>
              </li>
            ))}
          </ul>,
          document.body,
        )}
    </div>
  );
}
