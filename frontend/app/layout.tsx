import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Connect Analyzer",
  description: "Análisis de datos de ventas — prototipo con datos simulados",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}
