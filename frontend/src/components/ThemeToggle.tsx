import { Moon, Sun } from "lucide-react";
import { useTheme } from "../features/theme/ThemeContext";

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  const isDark = theme === "dark";

  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={toggleTheme}
      aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
    >
      <Sun size={22} className={`theme-toggle-icon${isDark ? "" : " theme-toggle-icon--active"}`} aria-hidden="true" />
      <Moon size={22} className={`theme-toggle-icon${isDark ? " theme-toggle-icon--active" : ""}`} aria-hidden="true" />
    </button>
  );
}
