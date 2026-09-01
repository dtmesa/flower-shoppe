import { useEffect, type ReactNode } from "react";
import { CircleX } from "lucide-react";

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
  centeredTitle?: boolean;
}

export function Modal({ title, onClose, children, wide, centeredTitle }: ModalProps) {
  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", handleKey);
    return () => document.removeEventListener("keydown", handleKey);
  }, [onClose]);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className={`modal-panel${wide ? " modal-panel--wide" : ""}`}
        onClick={(event) => event.stopPropagation()}
      >
        <div className={`modal-header${centeredTitle ? " modal-header--centered" : ""}`}>
          <h2>{title}</h2>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Close">
            <CircleX size={24} strokeWidth={2} aria-hidden="true" />
          </button>
        </div>
        <div className="modal-body">{children}</div>
      </div>
    </div>
  );
}
