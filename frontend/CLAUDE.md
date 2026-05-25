# frontend/ — Next.js (App Router) + Recharts

Reglas específicas del frontend. Las globales están en el `CLAUDE.md` de la raíz.

## App Router

- Rutas basadas en ficheros bajo `app/` (`app/page.tsx` → `/`, `app/layout.tsx` envuelve todo).
- **Server Components por defecto**: `page.tsx` es `async` y hace `await fetch` directamente (sin estado
  de carga). Metadata vía `export const metadata: Metadata = { ... }`.
- **Client Components** solo cuando hace falta interactividad/hooks/DOM: añadir `"use client"` arriba
  (los gráficos Recharts lo son).

## Llamadas al backend

- URL base: `process.env.BACKEND_URL ?? "http://localhost:5080"` (en compose es `http://backend:8080`).
- `fetch(..., { cache: "no-store" })` para datos siempre frescos; lanzar `Error` si `!res.ok`.
- Tipar las respuestas con genéricos (`fetchJson<ProductTotal[]>("/api/sales/by-product")`).

## Recharts

- Envolver en `<ResponsiveContainer>` + `<BarChart>`; `dataKey` debe coincidir con las claves del objeto
  (`product`, `totalAmount`). `Tooltip` formatea importes a 2 decimales.

## TypeScript

- `strict: true`. Tipos inline (`type ProductTotal = { product: string; totalAmount: number }`).
- Componentes en PascalCase; helpers en camelCase. Identificadores de código en **inglés**.
- **UI en español** (títulos, etiquetas). Preguntar antes de traducir/cambiar strings visibles.

## Testing (Vitest + React Testing Library)

- Ejecutar en CI: `npm run test:run` (watch: `npm run test`). Lint: `npm run lint`. Build: `npm run build`.
- Entorno jsdom. **Mockear `ResponsiveContainer`** (jsdom no calcula layout, Recharts no renderiza SVG útil):
  reemplazar por un `<div>` de tamaño fijo y comprobar que el árbol monta sin romper.
- `render()` + `getByTestId()` + matchers de `@testing-library/jest-dom`. Probar happy path y datos vacíos.
