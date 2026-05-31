# CLAUDE.md

Guía global para Claude Code en este repositorio. **Las reglas específicas de cada pieza viven en
su propio `CLAUDE.md`** (`backend/`, `frontend/`, `sap-mock/`) y se cargan al trabajar en esa carpeta.

## Proyecto

**SAP Analyzer** — herramienta interna para analizar grandes cantidades de datos provenientes de SAP
y visualizarlos en un frontal con gráficos. Se empieza a pequeña escala, con datos simulados, y se itera
poco a poco. No optimizar de más: nada de Kubernetes, colas ni microservicios.

## Arquitectura

Tres piezas, cada una en su carpeta en la raíz, orquestadas con **Docker Compose**:

- **`sap-mock/`** — Origen de datos simulado. nginx que sirve un `.txt` por HTTP. Host `8000` → interno
  **`8080`**. Imita lo que vendría de SAP; sustituible por SAP real (OData, ficheros, RFC/BAPI). → `sap-mock/CLAUDE.md`
- **`backend/`** — API en **.NET 10** con **arquitectura hexagonal** (Domain / Application+Ports /
  Infrastructure Inbound+Outbound). Host `5080` → interno **`8080`**. → `backend/CLAUDE.md`
- **`frontend/`** — **Next.js** (App Router) en `3000`. Consume la API y pinta gráficos. → `frontend/CLAUDE.md`

## Stack (no cambiar ni proponer alternativas)

- **Backend**: C# con **.NET 10** (Web API). **NO usar .NET 8 ni 9**.
- **Frontend**: **Next.js** + **TypeScript** + **App Router**. Gráficos con **Recharts**.
- **Orquestación**: **Docker** + **Docker Compose**.
- **PostgreSQL**: solo opción futura. **NO incluirlo todavía.**

## Regla de arquitectura sagrada: el origen de datos vive tras un puerto

El origen de los datos se aísla detrás del **puerto `ISalesRepository`** (`backend/Application/Ports/`).
Es el único contrato que conoce de dónde vienen los datos. Cada fuente concreta (hoy: mock vía
`MockTxtSalesRepository`, SAP real vía `SapODataSalesRepository`, tienda Shopify vía
`ShopifyOrdersRepository`; mañana SAP real vía RFC/BAPI o ficheros) es un **adaptador outbound** que
implementa ese puerto.

- El resto del backend y **todo el frontend dependen siempre del contrato, nunca de la implementación**.
- Cambiar el origen de datos debe poder hacerse **escribiendo un adaptador nuevo** y cambiando solo su
  registro en `Program.cs`; nada por encima del adaptador se entera.
- Esta regla es **inviolable**. Si una tarea parece requerir saltársela, parar y pedir confirmación.
- Detalles de capas y del patrón Result/Error: ver `backend/CLAUDE.md`.

## Convenciones del repositorio

- **Idioma**: identificadores de código (ficheros, tipos, funciones, campos, endpoints, namespaces) en
  **inglés**. Conversación, docs, `CLAUDE.md` y planes en **español**. Strings de UI: ver `frontend/CLAUDE.md`.
- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`…), mensaje en español.
- **Nunca commitear secretos** (`.env`, credenciales, tokens, API keys). Si se añade config con secretos,
  crear también un `.env.example` sin valores reales.
- **Nunca commitear datos reales** de SAP: van en `sap-mock/data-real/` o `sap-mock/private/` (ignorados)
  o fuera del repo. Los `.txt` **ficticios** de `sap-mock/data/` SÍ se commitean (fixtures pedagógicas).

## Comandos de desarrollo

- **Todo el stack**: `docker compose up --build` (desde la raíz).
- **Tests backend**: `./scripts/test-backend.sh` (dotnet test dockerizado, no requiere SDK local).
- **Frontend**: `cd frontend && npm run dev` | `npm run build` | `npm run lint` | `npm run test:run`.

## Gotchas conocidos (transversales)

- **CORS**: si el front no puede llamar al backend, casi siempre es CORS. El front corre en
  `localhost:3000`; los orígenes permitidos se configuran en `Cors:AllowedOrigins` (nunca `AllowAnyOrigin`).
- **Docker networking**: dentro de compose los servicios se llaman por **nombre de servicio** y **puerto
  interno** (`http://sap-mock:8080`), NUNCA `localhost` (que dentro de un contenedor es el propio contenedor).
- **Latin-1**: los ficheros del mock son ISO-8859-1, no UTF-8. Bug más típico con datos de SAP. Ver `sap-mock/CLAUDE.md`.
- **.NET 10 y auth**: los endpoints de API devuelven **401/403** directamente en lugar de redirigir al login.
  Es lo deseable para una API que sirve datos a un front, pero conviene saberlo si se añade auth.

## Documentación adicional

@plan-proyecto-sap.md
