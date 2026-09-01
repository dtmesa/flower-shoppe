/**
 * Plays the .field-invalid shake (see components.css) on the given elements, restarting it even
 * if it's already present from a previous validation error - remove, force a reflow, then
 * re-add, since React won't re-add a className that never actually changed (same trick used by
 * Header's scrollToContact for the wave).
 */
export function shakeFields(...elements: (HTMLElement | null)[]) {
  for (const el of elements) {
    if (!el) continue;
    el.classList.remove("field-invalid");
    void el.offsetWidth;
    el.classList.add("field-invalid");
  }
}
