import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import type { InventoryItem } from "../inventory/types";
import { useInventory } from "../inventory/inventoryApi";

const CART_STORAGE_KEY = "plumeria_cart";

export interface CartLine {
  item: InventoryItem;
  quantity: number;
}

interface CartContextValue {
  lines: CartLine[];
  itemCount: number;
  totalValue: number;
  isOpen: boolean;
  openCart: () => void;
  closeCart: () => void;
  addToCart: (item: InventoryItem, quantity: number) => void;
  updateQuantity: (itemId: string, quantity: number) => void;
  removeFromCart: (itemId: string) => void;
  clearCart: () => void;
}

const CartContext = createContext<CartContextValue | undefined>(undefined);

function loadStoredQuantities(): Record<string, number> {
  try {
    const raw = localStorage.getItem(CART_STORAGE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

export function CartProvider({ children }: { children: ReactNode }) {
  const { items } = useInventory();
  const [quantities, setQuantities] = useState<Record<string, number>>(loadStoredQuantities);
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(quantities));
  }, [quantities]);

  const lines = useMemo<CartLine[]>(() => {
    return Object.entries(quantities)
      .map(([itemId, quantity]) => {
        const item = items.find((candidate) => candidate.id === itemId);
        return item ? { item, quantity } : null;
      })
      .filter((line): line is CartLine => line !== null);
  }, [items, quantities]);

  const itemCount = lines.reduce((sum, line) => sum + line.quantity, 0);
  const totalValue = lines.reduce((sum, line) => sum + line.quantity * line.item.price, 0);

  const addToCart = useCallback((item: InventoryItem, quantity: number) => {
    setQuantities((prev) => {
      const nextQuantity = Math.min((prev[item.id] ?? 0) + quantity, item.quantityAvailable);
      return { ...prev, [item.id]: nextQuantity };
    });
    setIsOpen(true);
  }, []);

  const updateQuantity = useCallback((itemId: string, quantity: number) => {
    setQuantities((prev) => {
      if (quantity <= 0) {
        const { [itemId]: _removed, ...rest } = prev;
        return rest;
      }
      return { ...prev, [itemId]: quantity };
    });
  }, []);

  const removeFromCart = useCallback((itemId: string) => {
    setQuantities((prev) => {
      const { [itemId]: _removed, ...rest } = prev;
      return rest;
    });
  }, []);

  const clearCart = useCallback(() => setQuantities({}), []);

  const value: CartContextValue = {
    lines,
    itemCount,
    totalValue,
    isOpen,
    openCart: () => setIsOpen(true),
    closeCart: () => setIsOpen(false),
    addToCart,
    updateQuantity,
    removeFromCart,
    clearCart,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used within a CartProvider");
  return ctx;
}
