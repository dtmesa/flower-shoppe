import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StatusDropdown } from "./StatusDropdown";

/** The menu is portaled to <body>, so it is looked up there rather than in the render container. */
function menu() {
  return document.querySelector(".status-dropdown-menu");
}

/** Matched by class, not by name - "New" is also the label of one of the options once open. */
function trigger() {
  return document.querySelector<HTMLButtonElement>(".status-dropdown-trigger")!;
}

describe("StatusDropdown", () => {
  it("has no menu until it is opened", () => {
    render(<StatusDropdown value="NEW" onChange={vi.fn()} />);
    expect(menu()).not.toBeInTheDocument();
  });

  // Fading in is a plain on-mount CSS animation, so opening just puts the menu on screen.
  it("shows the menu as soon as it is opened", () => {
    render(<StatusDropdown value="NEW" onChange={vi.fn()} />);

    fireEvent.click(trigger());

    expect(menu()).toBeInTheDocument();
    expect(menu()).not.toHaveClass("status-dropdown-menu--leaving");
  });

  it("fades the menu out after a selection instead of dropping it", async () => {
    const onChange = vi.fn();
    render(<StatusDropdown value="NEW" onChange={onChange} />);

    await userEvent.click(trigger());
    expect(menu()).toBeInTheDocument();

    // The clickable element is the button inside the role="option" <li>, not the <li> itself.
    await userEvent.click(screen.getByRole("button", { name: "Confirmed" }));
    expect(onChange).toHaveBeenCalledWith("CONFIRMED");

    // Still on screen to animate against, and marked so the fade-out can play...
    expect(menu()).toBeInTheDocument();
    expect(menu()).toHaveClass("status-dropdown-menu--leaving");

    // ...and gone once the fade has had time to run.
    await waitFor(() => expect(menu()).not.toBeInTheDocument());
  });

  it("fades out when dismissed with Escape", async () => {
    render(<StatusDropdown value="NEW" onChange={vi.fn()} />);
    await userEvent.click(trigger());
    expect(menu()).toBeInTheDocument();

    fireEvent.keyDown(document, { key: "Escape" });

    expect(menu()).toHaveClass("status-dropdown-menu--leaving");
    await waitFor(() => expect(menu()).not.toBeInTheDocument());
  });

  // The trigger keeps reporting itself closed the instant it is dismissed, even while the menu
  // is still fading - the lingering element is presentation, not state.
  it("reports itself closed as soon as it is dismissed", async () => {
    render(<StatusDropdown value="NEW" onChange={vi.fn()} />);

    await userEvent.click(trigger());
    expect(trigger()).toHaveAttribute("aria-expanded", "true");

    fireEvent.keyDown(document, { key: "Escape" });
    expect(trigger()).toHaveAttribute("aria-expanded", "false");
  });
});
