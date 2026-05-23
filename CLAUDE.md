# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

**SAP Analyzer** — herramienta interna para analizar grandes cantidades de datos provenientes
de SAP y visualizarlos en un frontal con gráficos. Se empieza a pequeña escala, con datos
simulados, y se itera poco a poco.

## Arquitectura

Tres piezas, cada una en su carpeta en la raíz:

- **`sap-mock/`** — Origen de datos simulado (puerto **8000**). Imita lo que vendría de SAP.
  Sustituible en el futuro por SAP real (OData, ficheros, RFC/BAPI) sin tocar el resto.
- **`backend/`** — API en .NET 10 (puerto **5080**). Lee del mock, procesa y sirve REST al front.
- **`frontend/`** — Next.js (puerto **3000**). Consume la API y pinta gráficos.

Todo se orquesta con **Docker Compose**.

## Stack (no cambiar ni proponer alternativas)

- **Backend**: C# con **.NET 10** (Web API). .NET 10 es LTS. **NO usar .NET 8 ni 9**.
- **Frontend**: **Next.js** con **TypeScript** y **App Router**. Gráficos con **Recharts**.
- **Orquestación**: **Docker** + **Docker Compose**.
- **PostgreSQL**: solo como opción futura. **NO incluirlo todavía.**

## Regla de arquitectura sagrada: `SapDataService`

El origen de los datos vive aislado en **una única clase del backend llamada `SapDataService`**.
Es la única pieza del sistema que sabe de dónde vienen los datos (hoy un mock, mañana SAP real
vía OData o ficheros).

- El resto del backend y todo el frontend dependen **siempre de una interfaz**, nunca de la
  implementación concreta.
- Cualquier cambio en el origen de datos (de mock a OData, de OData a ficheros, etc.) debe
  poder hacerse tocando únicamente `SapDataService`.
- Esta regla es **inviolable**. Si una tarea parece requerir saltársela, parar y pedir
  confirmación antes de continuar.

## Convenciones de datos del mock

- **Formato**: delimitado por barra vertical (`|`), muy típico de SAP. La primera línea es
  cabecera. Ejemplo: `FECHA|CLIENTE|PRODUCTO|CANTIDAD|IMPORTE`.
- **Codificación**: **Latin-1 (ISO-8859-1)**, NO UTF-8. SAP exporta así. Si al leer un fichero
  los acentos o la "ñ" salen mal, casi seguro es esto.
- **Fechas**: formato `YYYYMMDD` sin separadores (estilo SAP).

## Estrategia de iteración

Empezar por el flujo más fino que funcione de punta a punta: **un fichero `.txt` → un endpoint
→ un gráfico**. Una vez eso se vea en el navegador, engordar: más endpoints de análisis, más
gráficos, filtros, y solo después persistencia y origen real. No optimizar de más: nada de
Kubernetes, colas ni microservicios.

## Gotchas conocidos

- **CORS**: si el frontend no puede llamar al backend, casi siempre es CORS mal configurado en
  la API. El front corre en `localhost:3000`.
- **Docker networking**: dentro del compose, los servicios se llaman por **nombre de servicio**
  (`http://sap-mock:8000`), NUNCA por `localhost`. `localhost` dentro de un contenedor es el
  propio contenedor.
- **.NET 10 y autenticación**: los endpoints de API devuelven **401/403** directamente en lugar
  de redirigir al login. Es el comportamiento deseable para una API que sirve datos a un front,
  pero conviene saberlo si se añade auth.
- **Latin-1**: ver sección anterior. Es el bug más típico con ficheros de SAP.

## Comandos de desarrollo

**Aún no hay comandos disponibles**: el repositorio está en fase de esqueleto y ninguna de las
tres piezas tiene código. Esta sección se rellenará cuando se bootstrapee cada parte:

- Backend: `dotnet new webapi -n SapAnalytics` dentro de `backend/`.
- Frontend: `npx create-next-app@latest frontend` (TypeScript + App Router) + `npm install recharts`.
- Orquestación: `docker compose up --build` desde la raíz una vez existan los `Dockerfile`s.

Actualizar esta sección al añadir comandos reales de build/test/lint.

## Convenciones del repositorio

- **Nunca commitear secretos** (`.env`, credenciales, tokens, API keys).
- **Nunca commitear datos reales** procedentes de SAP. Los datos reales van en
  `sap-mock/data-real/` o `sap-mock/private/` (ambos ignorados por git) o fuera del repo.
- Los `.txt` **inventados** que viven en `sap-mock/data/` SÍ se commitean: son fixtures
  pedagógicas, no datos sensibles. Su contenido tiene que ser totalmente ficticio.
- Si se añade un fichero de configuración con secretos, crear también un `.env.example`
  documentando las variables sin valores reales.

## Documentación adicional

- [`plan-proyecto-sap.md`](./plan-proyecto-sap.md) — plan detallado por fases (mock, backend,
  frontend, dockerización, persistencia opcional, paso a SAP real, seguridad y despliegue).
