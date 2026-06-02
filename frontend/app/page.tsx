import Dashboard from "./components/Dashboard";
import { fetchDashboard } from "./lib/dashboard";

export default async function Page() {
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
