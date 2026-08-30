import { useMemo, useState } from "react";
import type { InventoryItem } from "./types";
import { useInventory } from "./inventoryApi";
import { extractErrorMessage } from "../../lib/apiClient";
import { FilterSidebar } from "./FilterSidebar";
import { InventoryCard } from "./InventoryCard";
import { InventoryDetailModal } from "./InventoryDetailModal";

export function CatalogPage() {
  const { items, error, isLoading } = useInventory();

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

  return (
    <>
    <div className="page">
      <div className="page-intro">
        <h1>Plumeria for Local Pickup</h1>
        <p>Browse what&apos;s currently available. Add items to your cart and request them for pickup.</p>
      </div>

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
          {isLoading && <p className="state-message">Loading inventory...</p>}
          {error && <p className="state-message state-message--error">{extractErrorMessage(error)}</p>}
          {!isLoading && !error && items.length === 0 && (
            <p className="state-message">No plumeria available right now — check back soon!</p>
          )}
          {!isLoading && !error && items.length > 0 && filteredItems.length === 0 && (
            <p className="state-message">No items match your filters.</p>
          )}

          <div className="inventory-grid">
            {filteredItems.map((item) => (
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

function ContactFooter() {
  return (
    <footer className="contact-footer">
      <div className="contact-footer-inner">
        <h2>Contact Us</h2>
        <p>Have a question about an order or availability? Reach out anytime.</p>
        <div className="contact-footer-details">
          <span>placeholder@flowershoppe.example</span>
          <span>(123) 456-7890</span>
        </div>
      </div>
    </footer>
  );
}
