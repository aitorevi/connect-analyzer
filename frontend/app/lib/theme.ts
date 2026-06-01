"use client";

import { useSyncExternalStore } from "react";

// Recharts colours are SVG presentation attributes (fill/stroke), where CSS var() does NOT
// resolve — so we keep concrete palettes here and pick light/dark from prefers-color-scheme.
// The HTML chrome (cards, tooltip, KPIs) still themes via CSS variables in globals.css.
export type ChartTheme = {
  series: string[];
  grid: string;
  axis: string;
  border: string;
  surface: string;
  cursor: string;
};

const LIGHT: ChartTheme = {
  series: ["#6366f1", "#0ea5e9", "#10b981", "#f59e0b", "#f43f5e", "#8b5cf6"],
  grid: "#eceef3",
  axis: "#6b7280",
  border: "#e3e6eb",
  surface: "#ffffff",
  cursor: "rgba(15, 23, 42, 0.05)",
};

const DARK: ChartTheme = {
  series: ["#818cf8", "#38bdf8", "#34d399", "#fbbf24", "#fb7185", "#a78bfa"],
  grid: "#232735",
  axis: "#9aa3b2",
  border: "#272b38",
  surface: "#171a23",
  cursor: "rgba(255, 255, 255, 0.06)",
};

const QUERY = "(prefers-color-scheme: dark)";

function subscribe(callback: () => void): () => void {
  if (typeof window === "undefined" || !window.matchMedia) return () => {};
  const mq = window.matchMedia(QUERY);
  mq.addEventListener("change", callback);
  return () => mq.removeEventListener("change", callback);
}

const getSnapshot = (): boolean =>
  typeof window !== "undefined" && !!window.matchMedia
    ? window.matchMedia(QUERY).matches
    : false;

const getServerSnapshot = (): boolean => false;

// Subscribes to prefers-color-scheme via an external store — the idiomatic, SSR-safe way
// (server renders light, client syncs on hydration and live-updates on OS theme change).
export function useChartTheme(): ChartTheme {
  const dark = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
  return dark ? DARK : LIGHT;
}
