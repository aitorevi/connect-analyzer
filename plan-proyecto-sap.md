# Plan de proyecto: Entorno de prueba para análisis de datos de SAP

Objetivo: montar a pequeña escala el flujo completo **origen de datos (SAP simulado) → backend C# → frontend Next con gráficos**, todo orquestado con Docker Compose. Cada pieza es sustituible, de modo que el día que haya acceso a un SAP real solo se cambia el origen de datos.

## Decisiones de partida

- **No montamos un SAP real al principio.** Levantar un SAP de evaluación (SAP ABAP Platform Trial) pesa decenas de GB y necesita 16-32 GB de RAM. Para aprender el flujo no aporta. En su lugar montamos un **mock de SAP**: un servicio pequeño que expone datos de ejemplo vía OData/REST y/o ficheros `.txt`, imitando lo que haría SAP.
- **Backend en C# (.NET 10, Web API).** .NET 10 es LTS, con soporte hasta noviembre de 2028. Se elige sobre .NET 8/9 porque ambas alcanzan su fin de soporte en noviembre de 2026, demasiado pronto para un proyecto que empieza ahora.
- **Frontend en Next.js** con **Recharts** para los gráficos (sencillo y cómodo con React). ECharts queda como alternativa si luego se necesitan visualizaciones más pesadas.
- **PostgreSQL opcional**, desactivado al principio. Se añade cuando haga falta persistir datos.
- Todo levantable con un único `docker compose up`.

## Arquitectura

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  sap-mock       │     │  backend (C#)    │     │  frontend (Next)│
│  Expone datos   │────▶│  Lee del mock,   │────▶│  Consume la API │
│  OData/REST y   │     │  procesa y sirve │     │  y pinta los    │
│  ficheros .txt  │     │  una API REST    │     │  gráficos       │
└─────────────────┘     └──────────────────┘     └─────────────────┘
       (:8000)                 (:5080)                  (:3000)
                                  │
                          ┌───────┴────────┐
                          │ PostgreSQL     │  (opcional, fase 5)
                          │ (:5432)        │
                          └────────────────┘
```

## Estructura de carpetas

```
proyecto-sap/
├── docker-compose.yml
├── sap-mock/
│   ├── Dockerfile
│   ├── data/
│   │   ├── ventas.txt          # fichero de ancho fijo o delimitado por |
│   │   └── clientes.txt
│   └── (servicio que sirve esos datos)
├── backend/
│   ├── Dockerfile
│   ├── SapAnalytics.csproj
│   ├── Program.cs
│   ├── Models/
│   │   └── Venta.cs
│   ├── Services/
│   │   └── SapDataService.cs   # aquí se aísla el origen de datos
│   └── Controllers/
│       └── VentasController.cs
└── frontend/
    ├── Dockerfile
    ├── package.json
    ├── next.config.js
    └── app/
        ├── page.tsx
        └── components/
            └── GraficoVentas.tsx
```

La clave del diseño: **`SapDataService` es la única pieza que sabe de dónde vienen los datos.** Hoy lee del mock; mañana leerá de OData real, de RFC/BAPI o de ficheros. El resto del backend y todo el frontend no cambian.

## Fases de ejecución

### Fase 0 — Preparar el terreno
1. Crear la estructura de carpetas de arriba.
2. Instalar (si no están): Docker + Docker Compose, .NET 10 SDK, Node.js 20+.
3. Inicializar git en la raíz.

### Fase 1 — Mock de SAP con datos de ejemplo
1. Crear ficheros `.txt` de ejemplo en `sap-mock/data/`. Empezar con formato **delimitado por barra vertical (`|`)**, que es muy típico de SAP. Ejemplo de `ventas.txt`:
   ```
   FECHA|CLIENTE|PRODUCTO|CANTIDAD|IMPORTE
   20260101|C001|Producto A|10|1500.00
   20260102|C002|Producto B|5|750.50
   20260103|C001|Producto C|8|1200.00
   ```
   Nota sobre la codificación: SAP suele exportar en **Latin-1 (ISO-8859-1)**, no en UTF-8. Tenerlo presente al leer ficheros, porque los acentos y la "ñ" pueden salir mal.
2. Montar un servicio mínimo que sirva esos ficheros por HTTP. Dos opciones:
   - **Simple:** un contenedor que solo sirve los `.txt` como archivos estáticos (incluso un `nginx`).
   - **Más realista:** un pequeño servicio que exponga un endpoint OData-like (`/odata/Ventas`) devolviendo JSON, para ensayar el caso S/4HANA. Para empezar vale la opción simple.

### Fase 2 — Backend en C# (.NET 10 Web API)
1. Crear el proyecto Web API: `dotnet new webapi -n SapAnalytics` (generará el proyecto apuntando a `net10.0` en el `.csproj`).
2. **Modelo** `Venta.cs`: clase con Fecha, Cliente, Producto, Cantidad, Importe.
3. **`SapDataService`**: clase responsable de leer del mock. Primera versión: descarga el `.txt` del `sap-mock`, lo parsea (split por `|`, saltando la cabecera, parseando tipos y respetando la codificación Latin-1) y devuelve `List<Venta>`. Aislar aquí la lógica de origen es lo más importante del proyecto.
4. **`VentasController`**: expone endpoints REST limpios para el front, por ejemplo:
   - `GET /api/ventas` → todas las ventas
   - `GET /api/ventas/por-producto` → agregado de importe por producto (esto es ya "análisis": agrupar y sumar)
   - `GET /api/ventas/por-cliente` → agregado por cliente
5. Habilitar **CORS** para que el frontend (`localhost:3000`) pueda llamar a la API.
6. Probar con `curl` o el navegador que los endpoints devuelven JSON correcto.

### Fase 3 — Frontend en Next.js
1. Crear el proyecto: `npx create-next-app@latest frontend` (con TypeScript y App Router).
2. Instalar Recharts: `npm install recharts`.
3. **`page.tsx`**: hace `fetch` a `http://localhost:5080/api/ventas/por-producto` y pasa los datos al componente de gráfico.
4. **`GraficoVentas.tsx`**: un `BarChart` de Recharts mostrando importe por producto. Añadir después un segundo gráfico (por ejemplo, línea de importe por fecha) para tener un mini-dashboard.
5. Añadir algún filtro sencillo (por cliente o por rango de fechas) para empezar a notar la parte interactiva.

### Fase 4 — Dockerizar y orquestar
1. **Dockerfile del backend**: imagen multi-stage con el SDK de .NET 10 para compilar (`mcr.microsoft.com/dotnet/sdk:10.0`) y el runtime de ASP.NET para ejecutar (`mcr.microsoft.com/dotnet/aspnet:10.0`). Nota: en .NET 10 las imágenes base por defecto pasan a basarse en Ubuntu.
2. **Dockerfile del frontend**: build de Next y servir en producción (o modo dev para iterar).
3. **`docker-compose.yml`** con los tres servicios (`sap-mock`, `backend`, `frontend`), su red común y los puertos expuestos. El backend apunta al `sap-mock` por su nombre de servicio dentro de la red de Docker (no `localhost`).
4. Levantar todo con `docker compose up --build` y verificar el flujo de punta a punta en el navegador.

### Fase 5 — Persistencia (opcional, cuando haga falta)
1. Añadir un servicio **PostgreSQL** al compose.
2. En el backend, usar **Entity Framework Core** para guardar las ventas leídas del mock, de modo que el análisis no dependa de releer el fichero cada vez.
3. Esto prepara el terreno para cuando los datos vengan de SAP en cargas periódicas.

### Fase 6 — Acercarse al SAP real
1. Averiguar la **versión de SAP** del entorno real: si es **S/4HANA**, lo normal es tener servicios **OData** (REST + JSON con `$filter`, `$select`...), que encajan de maravilla con este montaje. Si es **ECC o anterior**, tocará **RFC/BAPI** (conector **NCo** para .NET) o seguir con ficheros.
2. Crear una segunda implementación de `SapDataService` que en vez del mock lea del origen real (OData o NCo). Como el resto del código depende de la interfaz y no de la implementación, **se cambia esta pieza y nada más**.
3. Conseguir un fichero `.txt` real de ejemplo cuanto antes y abrirlo en un editor para confirmar formato exacto (delimitador, ancho fijo, codificación) y ajustar el parser.

### Fase 7 — Seguridad

La regla general: **en la fase de prototipo con datos inventados casi no hay nada que proteger; en cuanto entran datos reales de SAP, la seguridad deja de ser solo cosa tuya y pasa a ser una conversación con IT y los administradores de SAP.** Lo único innegociable desde el día uno es no filtrar secretos ni datos reales por git.

**Desde el día uno (prototipo):**
1. **Nada de secretos ni datos reales en git.** Un `.gitignore` que excluya `.env`, los ficheros `.txt` de datos y cualquier credencial. Los secretos van en variables de entorno; un `.env` local basta para empezar, siempre fuera del repositorio.
2. **Usar datos inventados o anonimizados** en el entorno de prueba, nunca datos reales de clientes si se puede evitar.

**Cuando entran datos reales / pasa a algo serio:**
3. **Datos sensibles y RGPD.** Las ventas y, sobre todo, los datos de clientes son confidenciales y pueden contener datos personales (RGPD). Definir quién puede ver qué.
4. **Gestión de secretos.** Pasar de `.env` a un gestor real: Docker secrets, el del proveedor cloud, o Azure Key Vault. Las credenciales contra SAP/BD/OData nunca en código ni en el `docker-compose.yml`.
5. **Proteger la propia aplicación:** autenticación (que solo entre quien deba), **HTTPS** siempre, y **CORS** restringido a tu frontal. Si es herramienta interna, integrar con el identity provider de la empresa (típicamente **Azure AD / Entra ID** en entornos Microsoft/SAP) en vez de un login propio.
6. **Permisos mínimos en SAP.** El usuario con el que la app accede a SAP debe tener solo los permisos imprescindibles (leer lo que necesita, nada más). Si la app solo lee ventas, su usuario no debería poder tocar otros módulos ni modificar nada.
7. **Red.** SAP suele vivir en la red interna; evitar exponer la conexión a SAP hacia internet. La conectividad (despliegue interno, VPN...) se define con IT.

### Fase 8 — Despliegue

1. **Prototipo / demo:** como todo está dockerizado, las plataformas que entienden contenedores son lo más rápido. **Railway**, **Render** o **Fly.io**: subes los contenedores, te dan una URL pública, gratis o barato para algo pequeño. Ideal para enseñárselo al compañero y validar la idea.
2. **Versión interna / real:** como los datos vienen de SAP (normalmente en red interna y no expuesto a internet), lo natural es desplegar **dentro de la infraestructura de la empresa**: un servidor o VM interna con el mismo `docker compose`, o los servicios de contenedores de su nube corporativa (**Azure** es lo más habitual en entornos SAP/Microsoft, también **AWS** o **GCP**). Esta decisión se toma con IT.
3. **Aviso de conectividad:** si se despliega fuera de la empresa pero el SAP está en red interna, el backend no podrá llegar al SAP. Es la razón principal por la que la versión real suele acabar viviendo dentro de la empresa. Para prototipar con datos de ejemplo, cualquier sitio vale.

## Orden recomendado para iterar

Empezar por el flujo más fino posible que funcione de punta a punta: **un solo fichero txt → un endpoint → un gráfico**. Una vez eso funcione y se vea en el navegador, ir engordando: más endpoints de análisis, más gráficos, filtros, persistencia y, finalmente, el origen real. Tener algo funcionando pronto y crecer poco a poco, como querías.

## Notas y advertencias prácticas

- **Codificación Latin-1**: el error más típico al leer txt de SAP. Si ves caracteres raros, es esto.
- **CORS**: si el front no puede llamar al back, casi siempre es CORS mal configurado.
- **.NET 10, endpoints de API**: si más adelante añades autenticación, ten en cuenta que en .NET 10 los endpoints de API ya no hacen redirect al login, sino que devuelven 401/403 directamente. Para una API que sirve datos a un front es el comportamiento deseable, pero conviene saberlo.
- **Nombres de servicio en Docker**: dentro del compose, el backend llama al mock por su nombre de servicio (p. ej. `http://sap-mock:8000`), no por `localhost`.
- **No optimizar de más al principio**: nada de Kubernetes, colas, ni microservicios. Tres contenedores y a iterar.
- **Acceso al SAP real**: el cómo (OData / RFC / ficheros) lo decide el equipo que administra el SAP. Conviene hablar con ellos pronto para saber qué van a permitir, porque condiciona la Fase 6.
