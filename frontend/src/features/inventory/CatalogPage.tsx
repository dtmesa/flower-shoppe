import { useMemo, useState, type CSSProperties } from "react";
import type { InventoryItem } from "./types";
import { useInventory } from "./inventoryApi";
import { useFadeSwap } from "../../lib/useFadeSwap";
import { FilterSidebar } from "./FilterSidebar";
import { InventoryCard } from "./InventoryCard";
import { InventoryDetailModal } from "./InventoryDetailModal";
import bannerImage from "../../assets/banner.jpg";

/** Keep in step with the `.inventory-grid--swapping` transition in components.css. */
const GRID_FADE_OUT_MS = 300;

export function CatalogPage() {
  const { items: allItems, error, isLoading } = useInventory();
  const items = useMemo(() => allItems.filter((item) => item.quantityAvailable > 0), [allItems]);

  const [selectedTypes, setSelectedTypes] = useState<string[]>([]);
  const [selectedColors, setSelectedColors] = useState<string[]>([]);
  const [selectedSizes, setSelectedSizes] = useState<string[]>([]);
  const [priceLimit, setPriceLimit] = useState<number | null>(null);

  const [selectedItem, setSelectedItem] = useState<InventoryItem | null>(null);

  const priceCeiling = useMemo(
    () => Math.ceil(items.reduce((max, item) => Math.max(max, item.price), 0)),
    [items],
  );

  function toggleType(type: string) {
    setSelectedTypes((prev) => (prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]));
  }

  function toggleColor(color: string) {
    setSelectedColors((prev) => (prev.includes(color) ? prev.filter((c) => c !== color) : [...prev, color]));
  }

  function toggleSize(size: string) {
    setSelectedSizes((prev) => (prev.includes(size) ? prev.filter((s) => s !== size) : [...prev, size]));
  }

  const filteredItems = useMemo(() => {
    const effectivePriceLimit = priceLimit ?? priceCeiling;
    return items.filter((item) => {
      const matchesType = selectedTypes.length === 0 || selectedTypes.includes(item.type);
      const matchesColor = selectedColors.length === 0 || (item.color && selectedColors.includes(item.color));
      const matchesSize = selectedSizes.length === 0 || (item.size && selectedSizes.includes(item.size));
      const matchesPrice = item.price <= effectivePriceLimit;
      return matchesType && matchesColor && matchesSize && matchesPrice;
    });
  }, [items, selectedTypes, selectedColors, selectedSizes, priceLimit, priceCeiling]);

  // What counts as "a new filter was applied" - the selections themselves, not the resulting
  // array, which gets a fresh identity whenever the inventory is refetched.
  const filterSignature = [
    selectedTypes.join(),
    selectedColors.join(),
    selectedSizes.join(),
    priceLimit,
  ].join("|");

  const {
    shownKey,
    shown: visibleItems,
    fadingOut,
  } = useFadeSwap(filterSignature, filteredItems, GRID_FADE_OUT_MS);

  return (
    <>
    <div className="banner">
      <img src={bannerImage} alt="" className="banner-image" />
      <div className="banner-overlay">
        <h1>Plumeria, From Our Garden to Yours</h1>
        <p>
          Discover what&apos;s currently available and reserve your choices for local pickup in Orange
          County, CA
        </p>
      </div>
    </div>
    <div className="page">

      <div className="catalog-layout">
        {items.length > 0 && (
          <FilterSidebar
            selectedTypes={selectedTypes}
            onToggleType={toggleType}
            selectedColors={selectedColors}
            onToggleColor={toggleColor}
            selectedSizes={selectedSizes}
            onToggleSize={toggleSize}
            priceCeiling={priceCeiling}
            priceLimit={priceLimit ?? priceCeiling}
            onPriceLimitChange={setPriceLimit}
          />
        )}

        <div className="catalog-content">
          {!isLoading && !error && items.length === 0 && (
            <p className="state-message">No plumeria available right now — check back soon!</p>
          )}
          {/* visibleItems, not filteredItems: during a swap the outgoing set is still on screen,
              and this would otherwise appear alongside it for the length of the fade-out. */}
          {!isLoading && !error && items.length > 0 && visibleItems.length === 0 && (
            <p className="state-message">No items match your filters.</p>
          )}

          {/* Keyed on the filter signature so a swap remounts the grid and replays its fade-in;
              the --swapping class fades the outgoing set out first (see useFadeSwap). */}
          <div
            key={shownKey}
            className={`inventory-grid${fadingOut ? " inventory-grid--swapping" : ""}`}
          >
            {visibleItems.map((item) => (
              <InventoryCard key={item.id} item={item} onSelect={() => setSelectedItem(item)} />
            ))}
          </div>
        </div>
      </div>

      {selectedItem && <InventoryDetailModal item={selectedItem} onClose={() => setSelectedItem(null)} />}
    </div>
    <ContactFooter />
    </>
  );
}

const CONTACT_TITLE = "Contact Us";

function ContactFooter() {
  return (
    <footer className="contact-footer" id="contact">
      <div className="contact-footer-inner">
        <h2 className="contact-footer-title">
          {CONTACT_TITLE.split("").map((char, index) => (
            <span key={index} className="wave-letter" style={{ "--i": index } as CSSProperties}>
              {char === " " ? " " : char}
            </span>
          ))}
        </h2>
        <p>Have a question about an order or availability?</p>
        <div className="contact-footer-details">
          <span>placeholder@flowershoppe.example</span>
          <span>(123) 456-7890</span>
        </div>
      </div>
    </footer>
  );
}
