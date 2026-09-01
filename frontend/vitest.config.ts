import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// Deliberately standalone rather than merged into vite.config.ts - see the note there. This
// file is excluded from `tsc -b` (tsconfig.node.json only includes vite.config.ts), so the
// nested-vite type clash never surfaces.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./src/test/setup.ts",
    // Only our own specs - without this the default glob also walks node_modules.
    include: ["src/**/*.test.{ts,tsx}"],
  },
});
