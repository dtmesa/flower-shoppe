import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { act, render, renderHook, screen, waitFor } from "@testing-library/react";
import { FormError, useDismissingError } from "./FormError";

describe("FormError", () => {
  const slot = (container: HTMLElement) => container.querySelector(".form-error-slot");

  // The slot is always in the DOM so it can animate in both directions; with no message it is a
  // collapsed, zero-height row rather than an absent element.
  it("stays collapsed when there is no message", () => {
    const { container } = render(<FormError message={null} />);

    expect(slot(container)).toBeInTheDocument();
    expect(slot(container)).not.toHaveClass("form-error-slot--open");
    expect(container.querySelector(".form-error-text")).toBeEmptyDOMElement();
  });

  it("opens and shows the message when there is one", () => {
    const { container } = render(<FormError message="Name and code are required." />);

    expect(slot(container)).toHaveClass("form-error-slot--open");
    expect(screen.getByText("Name and code are required.")).toBeInTheDocument();
  });

  // Clearing the message closes the slot, but the text has to outlive it - otherwise the box
  // empties out halfway through sliding shut.
  it("keeps the text through the closing slide, then drops it", async () => {
    const { container, rerender } = render(<FormError message="Boom" />);

    rerender(<FormError message={null} />);
    expect(slot(container)).not.toHaveClass("form-error-slot--open");
    expect(screen.getByText("Boom")).toBeInTheDocument();

    await waitFor(() => expect(screen.queryByText("Boom")).not.toBeInTheDocument());
  });

  it("applies the prominent colouring only when asked", () => {
    const { container, rerender } = render(<FormError message="x" />);
    expect(slot(container)).not.toHaveClass("form-error-slot--prominent");

    rerender(<FormError message="x" prominent />);
    expect(slot(container)).toHaveClass("form-error-slot--prominent");
  });
});

describe("useDismissingError", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("starts empty", () => {
    const { result } = renderHook(() => useDismissingError());
    expect(result.current[0]).toBeNull();
  });

  it("holds the message, then clears it after the dismiss delay", () => {
    const { result } = renderHook(() => useDismissingError());

    act(() => result.current[1]("Something went wrong"));
    expect(result.current[0]).toBe("Something went wrong");

    act(() => void vi.advanceTimersByTime(4999));
    expect(result.current[0]).toBe("Something went wrong");

    act(() => void vi.advanceTimersByTime(1));
    expect(result.current[0]).toBeNull();
  });

  // Re-reporting the same message should restart the countdown, not be swallowed as a no-op.
  it("restarts the timer when the same message is set again", () => {
    const { result } = renderHook(() => useDismissingError());

    act(() => result.current[1]("Duplicate"));
    act(() => void vi.advanceTimersByTime(4000));
    act(() => result.current[1]("Duplicate"));

    act(() => void vi.advanceTimersByTime(4000));
    expect(result.current[0]).toBe("Duplicate");

    act(() => void vi.advanceTimersByTime(1000));
    expect(result.current[0]).toBeNull();
  });

  it("can be cleared manually", () => {
    const { result } = renderHook(() => useDismissingError());

    act(() => result.current[1]("Transient"));
    act(() => result.current[1](null));
    expect(result.current[0]).toBeNull();
  });
});
