import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import Sparkline from "./Sparkline";

describe("Sparkline", () => {
  it("renders an svg with a polyline for a series", () => {
    const { container } = render(<Sparkline values={[1, 5, 3, 8]} />);

    expect(container.querySelector("svg.sparkline")).not.toBeNull();
    expect(container.querySelector("polyline")).not.toBeNull();
  });

  it("renders nothing for fewer than two points", () => {
    const { container } = render(<Sparkline values={[5]} />);

    expect(container.querySelector("svg")).toBeNull();
  });
});
