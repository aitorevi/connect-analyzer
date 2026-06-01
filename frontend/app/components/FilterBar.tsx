"use client";

import type { Filters } from "../lib/analytics";
import MultiSelect from "./MultiSelect";

type Props = {
  filters: Filters;
  onChange: (next: Filters) => void;
  onReset: () => void;
  range: { min: string | null; max: string | null };
  productOptions: string[];
  customerOptions: string[];
  active: boolean;
};

export default function FilterBar({
  filters,
  onChange,
  onReset,
  range,
  productOptions,
  customerOptions,
  active,
}: Props) {
  return (
    <section className="card filter-bar" aria-label="Filtros">
      <div className="filter-field">
        <label className="filter-field__label" htmlFor="filter-from">
          Desde
        </label>
        <input
          id="filter-from"
          type="date"
          className="filter-input"
          min={range.min ?? undefined}
          max={range.max ?? undefined}
          value={filters.from ?? range.min ?? ""}
          onChange={(e) => onChange({ ...filters, from: e.target.value || null })}
        />
      </div>

      <div className="filter-field">
        <label className="filter-field__label" htmlFor="filter-to">
          Hasta
        </label>
        <input
          id="filter-to"
          type="date"
          className="filter-input"
          min={range.min ?? undefined}
          max={range.max ?? undefined}
          value={filters.to ?? range.max ?? ""}
          onChange={(e) => onChange({ ...filters, to: e.target.value || null })}
        />
      </div>

      <div className="filter-field">
        <span className="filter-field__label">Productos</span>
        <MultiSelect
          label="Productos"
          options={productOptions}
          selected={filters.products}
          onChange={(products) => onChange({ ...filters, products })}
        />
      </div>

      <div className="filter-field">
        <span className="filter-field__label">Clientes</span>
        <MultiSelect
          label="Clientes"
          options={customerOptions}
          selected={filters.customers}
          onChange={(customers) => onChange({ ...filters, customers })}
        />
      </div>

      <button
        type="button"
        className="filter-reset"
        onClick={onReset}
        disabled={!active}
      >
        Limpiar filtros
      </button>
    </section>
  );
}
