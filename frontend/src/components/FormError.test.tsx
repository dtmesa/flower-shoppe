import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { act, render, renderHook, screen } from "@testing-library/react";
import { FormError, useDismissingError } from "./FormError";

describe("FormError", () => {
  it("renders nothing when there is no message", () => {
    const { container } = render(<FormError message={null} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("renders the message when there is one", () => {
    render(<FormError message="Name and code are required." />);
    expect(screen.getByText("Name and code are required.")).toBeInTheDocument();
  });

  // The reserved variant stays mounted even while empty, so showing/hiding it fades in place
  // rather than shifting the content underneath it.
  it("stays mounted while empty when reserving space", () => {
    const { container } = render(<FormError message={null} reserveSpace />);
    const el = container.querySelector(".form-error");
    expect(el).toBeInTheDocument();
    expect(el).toHaveClass("form-error--reserved");
    expect(el).not.toHaveClass("form-error--visible");
  });

  it("marks itself visible once it has a message", () => {
    const { container } = render(<FormError message="Boom" reserveSpace />);
    expect(container.querySelector(".form-error")).toHaveClass("form-error--visible");
  });

  it("applies the compact modifier only when asked", () => {
    const { container, rerender } = render(<FormError message="x" reserveSpace />);
    expect(container.querySelector(".form-error")).not.toHaveClass("form-error--compact");

    rerender(<FormError message="x" reserveSpace compact />);
    expect(container.querySelector(".form-error")).toHaveClass("form-error--compact");
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
