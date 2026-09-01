import type { CSSProperties } from "react";
import { Check } from "lucide-react";
import { useCategories } from "./categoriesApi";
import { formatWholeDollars } from "../../lib/format";

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
  const { types, colors, sizes } = useCategories();

  return (
    <aside className="filter-sidebar">
      <div className="filter-section">
        <h3>Type</h3>
        {types.map((type) => (
          <FilterCheckboxOption
            key={type.id}
            label={type.name}
            checked={selectedTypes.includes(type.name)}
            onChange={() => onToggleType(type.name)}
          />
        ))}
      </div>

      <div className="filter-section">
        <h3>Color</h3>
        {colors.map((color) => (
          <FilterCheckboxOption
            key={color.id}
            label={color.name}
            checked={selectedColors.includes(color.name)}
            onChange={() => onToggleColor(color.name)}
          />
        ))}
      </div>

      <div className="filter-section">
        <h3>Size</h3>
        {sizes.map((size) => (
          <FilterCheckboxOption
            key={size.id}
            label={size.name}
            checked={selectedSizes.includes(size.name)}
            onChange={() => onToggleSize(size.name)}
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
            {priceLimit >= priceCeiling ? <span className="filter-slider-infinity">∞</span> : formatWholeDollars(priceLimit)}
          </span>
        </div>
      </div>
    </aside>
  );
}
