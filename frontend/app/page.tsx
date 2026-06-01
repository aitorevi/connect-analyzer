import Dashboard from "./components/Dashboard";
import { fetchDashboard } from "./lib/dashboard";

export default async function Page() {
  // Best-effort initial fetch: instant charts when the backend is warm, empty (→ client
  // self-heal) when it is cold. Never throws, so the demo never lands on the error boundary.
  const { sales } = await fetchDashboard();

  return (
    <main>
      <header className="page-header">
        <h1>Connect Analyzer</h1>
        <p className="subtitle">Prototype with simulated sales data.</p>
      </header>

      <Dashboard initialSales={sales} />
    </main>
  );
}
