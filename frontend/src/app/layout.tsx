import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import { AuthProvider } from '@/components/auth/auth-provider';
import './globals.css';

const inter = Inter({
  variable: '--font-inter',
  subsets: ['latin'],
});

export const metadata: Metadata = {
  title: 'Geek SEO',
  description: 'AI-powered SEO content optimization',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={`${inter.variable} light h-full antialiased`} style={{ colorScheme: 'light' }}>
      <body className="flex min-h-full flex-col bg-[var(--color-bg)] text-[var(--color-text-primary)]">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
