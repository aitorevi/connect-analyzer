import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import FilterBar from "./FilterBar";
import { EMPTY_FILTERS } from "../lib/analytics";

const baseProps = {
  filters: EMPTY_FILTERS,
  range: { min: "2026-01-01", max: "2026-01-31" },
  productOptions: ["A", "B"],
  customerOptions: ["C1", "C2"],
};

describe("FilterBar", () => {
  it("disables the reset button when there are no active filters", () => {
    render(
      <FilterBar
        {...baseProps}
        active={false}
        onChange={vi.fn()}
        onReset={vi.fn()}
      />,
    );
    expect(screen.getByRole("button", { name: /Limpiar filtros/i })).toBeDisabled();
  });

  it("calls onReset when active and the reset button is clicked", () => {
    const onReset = vi.fn();
    render(
      <FilterBar {...baseProps} active onChange={vi.fn()} onReset={onReset} />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Limpiar filtros/i }));
    expect(onReset).toHaveBeenCalled();
  });

  it("propagates a date-range change via onChange", () => {
    const onChange = vi.fn();
    render(
      <FilterBar {...baseProps} active={false} onChange={onChange} onReset={vi.fn()} />,
    );
    fireEvent.change(screen.getByLabelText("Desde"), {
      target: { value: "2026-01-10" },
    });
    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({ from: "2026-01-10" }),
    );
  });
});
