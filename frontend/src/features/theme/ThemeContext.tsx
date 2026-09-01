import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

type Theme = "light" | "dark";

interface ThemeContextValue {
  theme: Theme;
  toggleTheme: () => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

// The blocking inline script in index.html already sets data-theme on <html> before first
// paint (avoids a flash of the wrong theme) - read that back here instead of recomputing it, so
// this initial render always matches what's already on screen.
function getInitialTheme(): Theme {
  const attr = document.documentElement.getAttribute("data-theme");
  return attr === "dark" ? "dark" : "light";
}

// Matches the fade duration set on body.theme-transitioning in index.css - the swap happens at
// the dimmest point of the fade so the instant attribute change (and any un-transitioned colors
// underneath it) is hidden rather than visible as a jump.
const THEME_FADE_MS = 320;

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);

  const value = useMemo<ThemeContextValue>(
    () => ({
      theme,
      toggleTheme: () => {
        const body = document.body;
        body.classList.add("theme-transitioning");
        window.setTimeout(() => {
          setTheme((prev) => {
            const next = prev === "dark" ? "light" : "dark";
            localStorage.setItem("theme", next);
            document.documentElement.setAttribute("data-theme", next);
            return next;
          });
          // A plain timeout rather than requestAnimationFrame - rAF callbacks are suspended on a
          // backgrounded/hidden tab, which would leave the page stuck invisible until it
          // regains focus.
          window.setTimeout(() => body.classList.remove("theme-transitioning"), 20);
        }, THEME_FADE_MS);
      },
    }),
    [theme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within a ThemeProvider");
  return ctx;
}
