import { useId, useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { Check, ChevronDown } from "lucide-react";
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

interface FilterSectionProps {
  title: string;
  children: ReactNode;
}

/* One collapsible filter group. The heading wraps the button rather than the other way round: a
   <button> may only contain phrasing content, so an <h3> nested inside one is invalid markup and
   costs the heading its place in the screen-reader outline. */
function FilterSection({ title, children }: FilterSectionProps) {
  const [open, setOpen] = useState(true);
  const bodyId = useId();

  return (
    <div className={`filter-section${open ? "" : " filter-section--collapsed"}`}>
      <h3>
        <button
          type="button"
          className="filter-section-toggle"
          onClick={() => setOpen((wasOpen) => !wasOpen)}
          aria-expanded={open}
          aria-controls={bodyId}
        >
          {/* Wrapped so hover scales the label alone - scaling the whole button drags the chevron
              (and its open/closed rotation) with it, the same split the status dropdown uses. */}
          <span className="filter-section-title">{title}</span>
          <ChevronDown size={14} strokeWidth={2.5} aria-hidden="true" className="filter-section-chevron" />
        </button>
      </h3>
      <div className="filter-section-body" id={bodyId}>
        <div className="filter-section-body-inner">{children}</div>
      </div>
    </div>
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
      <FilterSection title="Type">
        {types.map((type) => (
          <FilterCheckboxOption
            key={type.id}
            label={type.name}
            checked={selectedTypes.includes(type.name)}
            onChange={() => onToggleType(type.name)}
          />
        ))}
      </FilterSection>

      <FilterSection title="Color">
        {colors.map((color) => (
          <FilterCheckboxOption
            key={color.id}
            label={color.name}
            checked={selectedColors.includes(color.name)}
            onChange={() => onToggleColor(color.name)}
          />
        ))}
      </FilterSection>

      <FilterSection title="Size">
        {sizes.map((size) => (
          <FilterCheckboxOption
            key={size.id}
            label={size.name}
            checked={selectedSizes.includes(size.name)}
            onChange={() => onToggleSize(size.name)}
          />
        ))}
      </FilterSection>

      <FilterSection title="Max Price">
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
      </FilterSection>
    </aside>
  );
}
