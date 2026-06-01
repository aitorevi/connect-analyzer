import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Connect Analyzer",
  description: "SAP data analysis — prototype with simulated data",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
