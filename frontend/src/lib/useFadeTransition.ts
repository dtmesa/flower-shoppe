import { useEffect, useState } from "react";

/**
 * Lets something that only exists while it's shown still fade out on the way from the DOM.
 *
 * `mounted` outlives `show` going false by one fade, because unmounting immediately leaves
 * nothing on screen to animate and the element simply vanishes. `leaving` marks that window, for
 * a class that plays the fade-out.
 *
 * Fading *in* needs nothing from here: the element should carry a CSS animation that runs when
 * it mounts. That is deliberate rather than incidental - the obvious alternative, mounting it
 * transparent and flipping a class a frame later, silently breaks wherever animation frames
 * aren't being produced. A page can report `visibilityState: "visible"` and still never get a
 * frame (an occluded or non-compositing window), which would strand the element at opacity 0
 * instead of merely skipping the animation.
 *
 * `durationMs` only holds the element back from unmounting - the animations themselves live in
 * CSS, so the two need to be kept in step.
 */
export function useFadeTransition(show: boolean, durationMs: number) {
  const [mounted, setMounted] = useState(show);
  const [leaving, setLeaving] = useState(false);

  useEffect(() => {
    if (show) {
      setMounted(true);
      // Cancels a fade-out already under way, if this went back to shown mid-flight.
      setLeaving(false);
      return;
    }

    if (!mounted) return;

    setLeaving(true);
    const timer = setTimeout(() => {
      setMounted(false);
      setLeaving(false);
    }, durationMs);
    return () => clearTimeout(timer);
  }, [show, durationMs, mounted]);

  return { mounted, leaving };
}
