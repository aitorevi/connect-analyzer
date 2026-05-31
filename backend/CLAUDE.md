# backend/ — API .NET 10 (arquitectura hexagonal)

Reglas específicas del backend. Las globales están en el `CLAUDE.md` de la raíz.

## Arquitectura hexagonal (Ports & Adapters)

La dependencia **siempre apunta hacia el dominio**. Capas:

- **`Domain/`** — núcleo puro, sin conocimiento de framework ni infraestructura. Entidades y value
  objects (`Sale`, `ProductTotal`…), y los tipos transversales `Result.cs` / `Error.cs`.
- **`Application/`** — casos de uso que orquestan el dominio: `SalesAnalytics` (consultas) e `IngestSales`
  (ETL: lee de la fuente y persiste en el almacén).
  - **`Application/Ports/`** — interfaces que el exterior implementa: `ISalesRepository` (puerto de la
    **fuente** upstream) e `ISalesStore` (puerto del **almacén** propio).
- **`Infrastructure/Inbound/Http/`** — adaptador de entrada: controladores que traducen HTTP ↔ aplicación.
- **`Infrastructure/Outbound/`** — adaptadores de salida. Fuente: `MockTxt/MockTxtSalesRepository` (fichero
  del mock), `Sap/SapODataSalesRepository` (SAP S/4HANA real vía OData, sandbox del Business Accelerator
  Hub) y `Shopify/ShopifyOrdersRepository` (tienda real de Shopify vía Admin REST, con
  `Shopify/ShopifyTokenProvider` que cachea el access token del Client Credentials Grant).
  Almacén: `Sqlite/SqliteSalesStore` (SQLite con SQL a mano, sin ORM).
- **`Program.cs`** — composición/DI: cablea cada puerto con su adaptador concreto.

### Flujo de datos (dos puertos outbound)
La fuente y el almacén están desacoplados: `IngestSales` lee de la **fuente** (`ISalesRepository`,
mock o SAP) y guarda en el **almacén** (`ISalesStore`, SQLite), encadenando con `BindAsync`; la analítica
(`SalesAnalytics`) lee **solo del almacén**. La ingesta se dispara al arrancar (seed) y vía
`POST /api/sales/refresh`. Cambiar la fuente o la tecnología del almacén no toca dominio ni aplicación.

### Regla sagrada del origen de datos
`ISalesRepository` es el **único** contrato que conoce el origen de datos. Añadir una fuente nueva (OData,
RFC, ficheros) = escribir un adaptador outbound que lo implemente y cambiar **solo** su registro en
`Program.cs`. Nada por encima del adaptador conoce la fuente. Regla **inviolable**: si algo parece requerir
saltársela, parar y preguntar.

**Selección de fuente por config**: `SalesSource` (`Mock` por defecto | `Sap` | `Shopify`) elige qué
adaptador se registra. `Sap` requiere el secreto `Sap:ApiKey` (env `Sap__ApiKey` o `dotnet user-secrets`;
nunca en git; ver `.env.example`); la cabecera `APIKey` se inyecta en el composition root, así que el
adaptador no la conoce. `Shopify` requiere `Shopify:StoreUrl`, `Shopify:ClientId` y el secreto
`Shopify:ClientSecret`; el `ShopifyTokenProvider` intercambia esas credenciales por un access token
(Client Credentials Grant) y lo cachea, así que el `ShopifyOrdersRepository` solo conoce el token —
no las credenciales.

## Gestión de errores: `Result<T>` / `Error`

Errores **esperables** son valores (`Result`), no excepciones. Las excepciones se reservan para lo
verdaderamente excepcional.

- **`Result<T>`** (`Domain/Result.cs`): unión sellada `Success(T)` / `Failure(Error)`. No expone el valor
  crudo; se accede solo por:
  - `Match(onSuccess, onFailure)` — abre el Result en el borde.
  - `Map(f)` — transforma el valor en Success; propaga el Failure sin tocarlo.
  - `Bind(f)` — encadena pasos que devuelven `Result` (corta en el primer Failure).
- **`Error`** (`Domain/Error.cs`): record con `ErrorType { NotFound, Validation, Unauthorized, Unavailable, Unexpected }`
  + mensaje. Factories: `Error.NotFound(...)`, `Error.Validation(...)`, `Error.Unauthorized(...)`,
  `Error.Unavailable(...)`, `Error.Unexpected(...)`.

**Flujo por capas (no romperlo):**
1. **Adaptador outbound**: captura las excepciones de infraestructura **en su borde** (p.ej.
   `HttpRequestException` → `Error.Unavailable`) y devuelve `Result`. `OperationCanceledException` se
   **re-lanza** (la cancelación no es error de negocio). Del adaptador hacia arriba nadie ve excepciones.
2. **Aplicación**: encadena con `Map`/`Bind` **sin desenvolver**; nunca hace `try/catch` ni inspecciona el error.
3. **Controlador (inbound)**: **único sitio que abre el Result** con `Match` → `Ok(value)` o el traductor de error.
4. **`Infrastructure/Inbound/Http/ErrorHttpResults`**: **único punto** de traducción `Error` → HTTP
   (`ProblemDetails`/RFC 7807). Mapeo: `NotFound`→404, `Validation`→400, `Unauthorized`→401,
   `Unavailable`→502, `Unexpected`→500. El dominio nunca conoce HTTP.

## Estilo C#

- **File-scoped namespaces** (`namespace ConnectAnalytics.Domain;`), sin llaves.
- **Primary constructors siempre** para clases con dependencias: `public class SalesController(ISalesRepository sales)`.
  Referenciar el parámetro por su nombre (sin `_`, sin campo declarado). No usar la forma verbosa
  constructor + campo privado.
- **`record`** para datos inmutables (`Sale`, `Error`, `ProductTotal`, stubs de test). **`class sealed`**
  para adaptadores/servicios.
- `#nullable enable` (ya activo en el `.csproj`).
- **Cultura invariante** al parsear/formatear decimales y fechas (`1234.56`, fechas `YYYYMMDD`).
- `async Task<T>` con `CancellationToken`; **nunca** `.Result` ni `.Wait()` (no bloquear).
- Identificadores en **inglés** (`Sale` no `Venta`, `/api/sales/by-product`). El adaptador mapea las
  columnas en español del fichero del mock a los campos en inglés del dominio.
- **.NET 10 only** (no 8/9).

## Testing (xUnit)

Ejecutar: **`./scripts/test-backend.sh`** (desde la raíz; `dotnet test` dockerizado, reenvía args como `--filter`).

- Estructura espejo de `src` bajo `tests/ConnectAnalytics.Tests/`: `Domain/`, `Application/`, `Api/`,
  `Infrastructure/{Inbound,Outbound}/`, `TestDoubles/`.
- `[Fact]` para casos concretos; `[Theory]` + `[InlineData]` para parametrizar (p.ej. `ErrorType` → status).
- **Naming**: `Method_Condition_Expected` (`Map_OnFailure_DoesNotRunTransformAndPropagatesErrorUnchanged`).
- **Arrange-Act-Assert**.
- **Test doubles** (no librerías de mocking): stubs con factories estáticas
  (`StubSalesRepository.Returning(...)` / `.Failing(error)`) y fakes inline.
- **Integración HTTP**: `WebApplicationFactory<Program>` + `services.RemoveAll(typeof(ISalesRepository))`
  y `AddSingleton` del fake. `Program` se expone como `public partial class Program;` para esto.
- Helpers `Unwrap`/`UnwrapError` abren el `Result` vía `Match` en los tests.
