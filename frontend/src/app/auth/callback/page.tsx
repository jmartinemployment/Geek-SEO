'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { PKCE_STORAGE_KEY } from '@/lib/auth/pkce';

function CallbackInner() {
  const params = useSearchParams();
  const router = useRouter();
  const { setAccessToken } = useAuth();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      const code = params.get('code');
      const verifier = sessionStorage.getItem(PKCE_STORAGE_KEY);
      sessionStorage.removeItem(PKCE_STORAGE_KEY);

      if (!code || !verifier) {
        setError('Missing authorization code or PKCE verifier.');
        return;
      }

      try {
        const res = await fetch('/api/auth/token', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            grantType: 'authorization_code',
            code,
            codeVerifier: verifier,
          }),
        });
        if (!res.ok) {
          const body = await res.text();
          let message = body;
          try {
            const parsed = JSON.parse(body) as { error?: string };
            if (parsed.error) message = parsed.error;
          } catch {
            /* use raw body */
          }
          throw new Error(message);
        }
        const data = (await res.json()) as { accessToken: string; expiresIn: number };
        setAccessToken(data.accessToken, data.expiresIn);
        router.replace('/app/projects');
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Sign-in failed');
      }
    })();
  }, [params, router, setAccessToken]);

  return (
    <main className="mx-auto max-w-md p-8">
      {error ? <p className="text-red-600">{error}</p> : <p className="text-zinc-600">Completing sign-in…</p>}
    </main>
  );
}

export default function AuthCallbackPage() {
  return (
    <Suspense fallback={<main className="p-8">Completing sign-in…</main>}>
      <CallbackInner />
    </Suspense>
  );
}
