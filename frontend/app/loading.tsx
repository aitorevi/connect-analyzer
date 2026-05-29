// Streamed by App Router while the Server Component on page.tsx awaits the backend.
export default function Loading() {
  return (
    <main className="loading-state" aria-busy="true">
      <p>Cargando datos...</p>
    </main>
  );
}
