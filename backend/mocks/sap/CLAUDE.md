# backend/mocks/sap/ — Origen de datos simulado (servicio `sap-mock`)

Reglas específicas del mock. Las globales están en el `CLAUDE.md` de la raíz.

## Qué es

nginx **unprivileged** (UID 101) que sirve `data/sales.txt` por HTTP en el puerto **8080** (host `8000`).
Imita un export de SAP. Es sustituible por SAP real sin tocar el resto: el backend solo depende del puerto
`ISalesRepository` (ver `backend/CLAUDE.md`).

## Formato de los datos (estilo SAP)

- **Delimitado por barra vertical** (`|`). La **primera línea es cabecera**.
- Orden de columnas fijo: `DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT`.
  (En las fixtures las cabeceras pueden ir en español, p.ej. `FECHA|CLIENTE|PRODUCTO|...`; el **adaptador**
  es quien mapea esas columnas a los campos en inglés del dominio.)
- **Fechas** en `YYYYMMDD`, sin separadores.
- **Codificación Latin-1 (ISO-8859-1)**, NO UTF-8. SAP exporta así. Si acentos o `ñ` salen mal al leer,
  casi seguro es esto. El adaptador del backend lee con ISO-8859-1.

## Datos

- Solo fixtures **totalmente ficticias** se commitean en `data/`. Su contenido no puede ser sensible.
- **Datos reales** de SAP → `data-real/` o `private/` (ambos ignorados por git) o fuera del repo. Nunca commitear datos reales.
