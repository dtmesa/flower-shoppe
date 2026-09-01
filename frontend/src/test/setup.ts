import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";

// Unmount anything rendered in a test before the next one runs, so tests can't see each
// other's DOM. Web storage is cleared too when the environment provides it - depending on the
// jsdom URL/origin it may be absent, which is fine for tests that don't touch it.
afterEach(() => {
  cleanup();
  globalThis.localStorage?.clear();
  globalThis.sessionStorage?.clear();
});
