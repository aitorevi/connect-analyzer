"use client";

import { useEffect } from "react";

// App Router error boundary. Catches errors thrown by app/page.tsx (e.g. the backend
// fetch failing) so the dashboard never crashes the UI. Must be a Client Component
// because Next passes the `reset` callback in.
type Props = {
  error: Error & { digest?: string };
  reset: () => void;
};

export default function Error({ error, reset }: Props) {
  useEffect(() => {
    // Surface for dev diagnostics. In production this would go to a real logger.
    console.error(error);
  }, [error]);

  return (
    <main className="error-state" role="alert">
      <h1>Algo ha fallado</h1>
      <p>
        No hemos podido cargar los datos del backend. Comprueba que la API está
        levantada y vuelve a intentarlo.
      </p>
      <button type="button" onClick={reset}>
        Reintentar
      </button>
    </main>
  );
}
