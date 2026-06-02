import Dashboard from "./components/Dashboard";
import { fetchDashboard } from "./lib/dashboard";

export default async function Page() {
  const { sales } = await fetchDashboard();

  return (
    <main>
      <header className="page-header">
        <h1>Connect Analyzer</h1>
        <p className="subtitle">Prototipo con datos de ventas simulados.</p>
      </header>

      <Dashboard sales={sales} />
    </main>
  );
}
