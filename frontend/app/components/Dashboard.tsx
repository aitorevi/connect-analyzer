"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import ByProductChart from "./ByProductChart";
import ByCustomerChart from "./ByCustomerChart";
import type { CustomerTotal, DashboardData, ProductTotal } from "../lib/dashboard";

type Props = {
  initialByProduct: ProductTotal[];
  initialByCustomer: CustomerTotal[];
};

const POLL_INTERVAL_MS = 5000;
const MAX_ATTEMPTS = 30; // ~2.5 min, covers a free-tier mock + backend cold start

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Wraps the charts and self-heals the demo: on a free-tier cold start the store is empty
// until the backend re-seeds from the (also sleeping) mock. If we render with no data, we
// trigger a refresh and poll until rows appear, instead of leaving a dead "No hay datos".
export default function Dashboard({ initialByProduct, initialByCustomer }: Props) {
  const initialEmpty =
    initialByProduct.length === 0 && initialByCustomer.length === 0;

  const [byProduct, setByProduct] = useState(initialByProduct);
  const [byCustomer, setByCustomer] = useState(initialByCustomer);
  const [warming, setWarming] = useState(initialEmpty);
  const [gaveUp, setGaveUp] = useState(false);
  const running = useRef(false);

  const hasData = byProduct.length > 0 || byCustomer.length > 0;

  // Triggers a re-ingestion, then polls until rows appear or we exhaust the attempts.
  // No synchronous state updates: every setState happens after an await.
  const poll = useCallback(async () => {
    if (running.current) return;
    running.current = true;

    // Fire-and-forget re-ingestion; the polling below picks up the result.
    fetch("/api/dashboard", { method: "POST" }).catch(() => {});

    for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
      await delay(POLL_INTERVAL_MS);
      try {
        const res = await fetch("/api/dashboard", { cache: "no-store" });
        if (res.ok) {
          const data: DashboardData = await res.json();
          if (data.byProduct.length > 0 || data.byCustomer.length > 0) {
            setByProduct(data.byProduct);
            setByCustomer(data.byCustomer);
            setWarming(false);
            running.current = false;
            return;
          }
        }
      } catch {
        // keep polling — a cold backend may still be waking up
      }
    }

    setWarming(false);
    setGaveUp(true);
    running.current = false;
  }, []);

  const retry = useCallback(() => {
    setGaveUp(false);
    setWarming(true);
    poll();
  }, [poll]);

  useEffect(() => {
    // Mount only: if SSR already delivered data we never warm up. Deferred so the async
    // work starts outside the effect body (no synchronous state update on mount).
    if (!initialEmpty) return;
    const id = setTimeout(poll, 0);
    return () => clearTimeout(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <>
      {warming && !hasData && (
        <p className="subtitle" role="status">
          Calentando la demo… el backend gratuito de Render tarda ~1 min en arrancar en frío.
          Reintentando solo.
        </p>
      )}
      {gaveUp && !hasData && (
        <p className="subtitle" role="alert">
          La demo sigue arrancando.{" "}
          <button type="button" onClick={retry}>
            Reintentar
          </button>
        </p>
      )}

      <h2>Total amount by product</h2>
      <div className="card">
        <ByProductChart data={byProduct} />
      </div>

      <h2>Total amount by customer</h2>
      <div className="card">
        <ByCustomerChart data={byCustomer} />
      </div>
    </>
  );
}
