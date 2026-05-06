import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import Navbar from '@/components/Navbar';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'InsightPulse - Real-Time BI Dashboard',
  description: 'Professional business intelligence platform',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="dark">
      <body className={`${inter.className} bg-slate-900 text-slate-100`}>
        <Navbar />
        <main className="min-h-screen bg-slate-900">
          {children}
        </main>
      </body>
    </html>
  );
}