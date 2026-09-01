import { ChevronDown, ChevronUp } from "lucide-react";

interface QuantityStepperProps {
  value: number;
  min: number;
  max: number;
  step?: number;
  onChange: (value: number) => void;
  ariaLabel: string;
  className?: string;
}

export function QuantityStepper({ value, min, max, step = 1, onChange, ariaLabel, className }: QuantityStepperProps) {
  function clamp(next: number) {
    onChange(Math.min(max, Math.max(min, next)));
  }

  return (
    <div className={`quantity-stepper${className ? ` ${className}` : ""}`}>
      <input
        type="number"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={(event) => clamp(Number(event.target.value))}
        aria-label={ariaLabel}
      />
      <div className="quantity-stepper-buttons">
        <button
          type="button"
          onClick={() => clamp(value + step)}
          disabled={value >= max}
          aria-label="Increase quantity"
        >
          <ChevronUp size={14} strokeWidth={2.5} aria-hidden="true" />
        </button>
        <button
          type="button"
          onClick={() => clamp(value - step)}
          disabled={value <= min}
          aria-label="Decrease quantity"
        >
          <ChevronDown size={14} strokeWidth={2.5} aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}
