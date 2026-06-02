"use client";

import { useEffect } from "react";

type Props = {
  error: Error & { digest?: string };
  reset: () => void;
};

export default function Error({ error, reset }: Props) {
  useEffect(() => {
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
