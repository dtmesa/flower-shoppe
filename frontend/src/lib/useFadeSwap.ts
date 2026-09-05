import { useEffect, useRef, useState } from "react";

/**
 * Holds onto what's currently displayed long enough to fade it out before swapping in something
 * new. Without the delay the old content is gone the instant the new content exists, and only a
 * fade *in* is possible.
 *
 * Changes are tracked by `key` rather than by `value`, so the caller decides what counts as a
 * swap - a new array with the same contents shouldn't restart the animation. A `value` that
 * changes while `key` stays put is taken as the same content being refreshed and is adopted
 * immediately, so a background refetch isn't held back waiting for a swap that never comes.
 *
 * `durationMs` is the fade-*out* only, and needs to match the CSS that plays it. Fading the new
 * content back in is the CSS's business: `shownKey` changes on every swap, so using it as a React
 * `key` remounts the element and replays whatever on-mount animation it carries.
 */
export function useFadeSwap<T>(key: string, value: T, durationMs: number) {
  // Read at swap time rather than captured when the fade started, so the content that lands is
  // the newest - not whatever was current when the user first touched a filter.
  const latest = useRef(value);
  latest.current = value;

  const [shown, setShown] = useState({ key, value });
  const [fadingOut, setFadingOut] = useState(false);

  useEffect(() => {
    if (key === shown.key) {
      if (!Object.is(value, shown.value)) {
        setShown({ key, value });
      }
      return;
    }

    setFadingOut(true);
    const timer = setTimeout(() => {
      setShown({ key, value: latest.current });
      setFadingOut(false);
    }, durationMs);
    return () => clearTimeout(timer);
  }, [key, value, shown, durationMs]);

  return { shownKey: shown.key, shown: shown.value, fadingOut };
}
