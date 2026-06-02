import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import ChartTooltip from "./ChartTooltip";

describe("ChartTooltip", () => {
  it("renders nothing when inactive", () => {
    const { container } = render(
      <ChartTooltip active={false} payload={[{ name: "x", value: 1 }]} />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("formats a single-series value with the label", () => {
    render(
      <ChartTooltip
        active
        label="Café Molido"
        payload={[{ name: "Ingresos", value: 1234.5 }]}
      />,
    );
    expect(screen.getByText("Café Molido")).toBeInTheDocument();
    expect(screen.getByText("1,234.5")).toBeInTheDocument();
  });

  it("lists each series for a multi-series payload", () => {
    render(
      <ChartTooltip
        active
        label="A"
        payload={[
          { name: "Ingresos", value: 100 },
          { name: "Unidades", value: 5 },
        ]}
      />,
    );
    expect(screen.getByText(/Ingresos:/)).toBeInTheDocument();
    expect(screen.getByText(/Unidades:/)).toBeInTheDocument();
  });
});
