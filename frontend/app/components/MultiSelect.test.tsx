import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import MultiSelect from "./MultiSelect";

describe("MultiSelect", () => {
  it("opens and toggles an option on", () => {
    const onChange = vi.fn();

    render(
      <MultiSelect
        label="Productos"
        options={["A", "B", "C"]}
        selected={[]}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: /Productos/i }));
    fireEvent.click(screen.getByLabelText("B"));

    expect(onChange).toHaveBeenCalledWith(["B"]);
  });

  it("clears the selection", () => {
    const onChange = vi.fn();

    render(
      <MultiSelect
        label="Clientes"
        options={["A", "B"]}
        selected={["A"]}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: /Clientes/i }));
    fireEvent.click(screen.getByRole("button", { name: /Limpiar selección/i }));

    expect(onChange).toHaveBeenCalledWith([]);
  });
});
