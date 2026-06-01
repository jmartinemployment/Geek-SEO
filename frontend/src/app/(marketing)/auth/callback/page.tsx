'use client';

import { AuthStartLink } from '@/components/auth/auth-start-link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';

function CallbackInner() {
  const params = useSearchParams();
  const router = useRouter();
  const { setAccessToken } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const exchangeStartedRef = useRef(false);

  useEffect(() => {
    void (async () => {
      if (exchangeStartedRef.current) return;
      exchangeStartedRef.current = true;

      const oauthError = params.get('error');
      if (oauthError) {
        setError(params.get('error_description') ?? oauthError);
        return;
      }

      const code = params.get('code');
      if (!code) {
        setError('Sign-in did not return an authorization code. Try Log in again.');
        return;
      }

      try {
        const res = await fetch('/api/auth/token', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            grantType: 'authorization_code',
            code,
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
      {error ? (
        <>
          <p className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
            {error}
          </p>
          <AuthStartLink className="mt-6 inline-block rounded-[var(--radius-button)] bg-[var(--color-accent)] px-4 py-2 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]">
            Log in
          </AuthStartLink>
        </>
      ) : (
        <p className="text-[var(--color-text-secondary)]">Completing sign-in…</p>
      )}
    </main>
  );
}

export default function AuthCallbackPage() {
  return (
    <Suspense fallback={<main className="p-8 text-[var(--color-text-secondary)]">Completing sign-in…</main>}>
      <CallbackInner />
    </Suspense>
  );
}
