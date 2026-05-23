import ByProductChart from "./components/ByProductChart";
import ByCustomerChart from "./components/ByCustomerChart";

type ProductTotal = { product: string; totalAmount: number };
type CustomerTotal = { customerId: string; totalAmount: number };

async function fetchJson<T>(path: string): Promise<T> {
  const backendUrl = process.env.BACKEND_URL ?? "http://localhost:5080";
  const res = await fetch(`${backendUrl}${path}`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Backend responded ${res.status} on ${path}`);
  }
  return res.json();
}

export default async function Page() {
  const [byProduct, byCustomer] = await Promise.all([
    fetchJson<ProductTotal[]>("/api/sales/by-product"),
    fetchJson<CustomerTotal[]>("/api/sales/by-customer"),
  ]);

  return (
    <main>
      <h1>SAP Analyzer</h1>
      <p className="subtitle">Prototype with simulated sales data.</p>

      <h2>Total amount by product</h2>
      <div className="card">
        <ByProductChart data={byProduct} />
      </div>

      <h2>Total amount by customer</h2>
      <div className="card">
        <ByCustomerChart data={byCustomer} />
      </div>
    </main>
  );
}
