import Link from 'next/link';
import type { ComponentProps } from 'react';

type AuthStartLinkProps = Omit<ComponentProps<typeof Link>, 'href' | 'prefetch'>;

/** OAuth start redirects off-origin; disable prefetch to avoid RSC fetch CORS errors. */
export function AuthStartLink(props: AuthStartLinkProps) {
  return <Link href="/api/auth/start" prefetch={false} {...props} />;
}
