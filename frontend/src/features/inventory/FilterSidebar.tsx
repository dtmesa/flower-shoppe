import type { CSSProperties } from "react";
import { Check } from "lucide-react";
import { COLOR_OPTIONS, SIZE_OPTIONS, TYPE_OPTIONS } from "./types";

function formatPrice(price: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(price);
}

interface FilterCheckboxOptionProps {
  label: string;
  checked: boolean;
  onChange: () => void;
}

function FilterCheckboxOption({ label, checked, onChange }: FilterCheckboxOptionProps) {
  return (
    <label className="filter-checkbox">
      <span className="checkbox-box">
        <input type="checkbox" checked={checked} onChange={onChange} />
        <Check size={12} strokeWidth={3} aria-hidden="true" />
      </span>
      <span className="filter-checkbox-label">{label}</span>
    </label>
  );
}

interface FilterSidebarProps {
  selectedTypes: string[];
  onToggleType: (type: string) => void;
  selectedColors: string[];
  onToggleColor: (color: string) => void;
  selectedSizes: string[];
  onToggleSize: (size: string) => void;
  priceCeiling: number;
  priceLimit: number;
  onPriceLimitChange: (value: number) => void;
}

export function FilterSidebar({
  selectedTypes,
  onToggleType,
  selectedColors,
  onToggleColor,
  selectedSizes,
  onToggleSize,
  priceCeiling,
  priceLimit,
  onPriceLimitChange,
}: FilterSidebarProps) {
  return (
    <aside className="filter-sidebar">
      <div className="filter-section">
        <h3>Type</h3>
        {TYPE_OPTIONS.map((type) => (
          <FilterCheckboxOption
            key={type}
            label={type}
            checked={selectedTypes.includes(type)}
            onChange={() => onToggleType(type)}
          />
        ))}
      </div>

      <div className="filter-section">
        <h3>Color</h3>
        {COLOR_OPTIONS.map((color) => (
          <FilterCheckboxOption
            key={color}
            label={color}
            checked={selectedColors.includes(color)}
            onChange={() => onToggleColor(color)}
          />
        ))}
      </div>

      <div className="filter-section">
        <h3>Size</h3>
        {SIZE_OPTIONS.map((size) => (
          <FilterCheckboxOption
            key={size}
            label={size}
            checked={selectedSizes.includes(size)}
            onChange={() => onToggleSize(size)}
          />
        ))}
      </div>

      <div className="filter-section">
        <h3>Max Price</h3>
        <input
          type="range"
          min={0}
          max={priceCeiling}
          step={1}
          value={priceLimit}
          onChange={(event) => onPriceLimitChange(Number(event.target.value))}
          className="filter-slider"
          style={{ "--fill": `${priceCeiling > 0 ? (priceLimit / priceCeiling) * 100 : 0}%` } as CSSProperties}
          aria-label="Maximum price"
        />
        <div className="filter-slider-range">
          <span>$0</span>
          <span>
            {priceLimit >= priceCeiling ? <span className="filter-slider-infinity">∞</span> : formatPrice(priceLimit)}
          </span>
        </div>
      </div>
    </aside>
  );
}
