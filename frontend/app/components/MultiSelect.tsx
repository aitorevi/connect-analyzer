"use client";

import { useEffect, useId, useRef, useState } from "react";

type Props = {
  label: string;
  options: string[];
  selected: string[];
  onChange: (next: string[]) => void;
};

export default function MultiSelect({ label, options, selected, onChange }: Props) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const ref = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const panelId = useId();

  const close = () => {
    setOpen(false);
    buttonRef.current?.focus();
  };

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (event: PointerEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") close();
    };
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const selectedSet = new Set(selected);
  const filtered = query
    ? options.filter((o) => o.toLowerCase().includes(query.toLowerCase()))
    : options;

  const toggle = (option: string) => {
    const next = new Set(selectedSet);
    if (next.has(option)) next.delete(option);
    else next.add(option);
    onChange([...next]);
  };

  return (
    <div className="multiselect" ref={ref}>
      <button
        ref={buttonRef}
        type="button"
        className="multiselect__button"
        aria-haspopup="true"
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((v) => !v)}
      >
        {label}
        <span className="multiselect__count">
          {selected.length > 0 ? selected.length : "todos"}
        </span>
      </button>

      {open && (
        <div className="multiselect__panel" id={panelId} aria-label={label}>
          <input
            type="search"
            className="multiselect__search"
            placeholder="Buscar…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            autoFocus
          />
          <div className="multiselect__options">
            {filtered.length === 0 && (
              <p className="multiselect__empty">Sin coincidencias</p>
            )}
            {filtered.map((option) => (
              <label className="multiselect__option" key={option}>
                <input
                  type="checkbox"
                  checked={selectedSet.has(option)}
                  onChange={() => toggle(option)}
                />
                <span>{option}</span>
              </label>
            ))}
          </div>
          {selected.length > 0 && (
            <button
              type="button"
              className="multiselect__clear"
              onClick={() => onChange([])}
            >
              Limpiar selección
            </button>
          )}
        </div>
      )}
    </div>
  );
}
