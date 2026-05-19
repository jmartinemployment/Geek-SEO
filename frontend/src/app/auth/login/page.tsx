'use client';

import { useState } from 'react';
import { authConfig } from '@/lib/auth/config';
import { generateCodeChallenge, generateCodeVerifier, PKCE_STORAGE_KEY } from '@/lib/auth/pkce';

export default function LoginPage() {
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function startLogin() {
    setLoading(true);
    setError(null);
    try {
      const verifier = generateCodeVerifier();
      sessionStorage.setItem(PKCE_STORAGE_KEY, verifier);
      const challenge = await generateCodeChallenge(verifier);
      const params = new URLSearchParams({
        client_id: authConfig.clientId,
        redirect_uri: authConfig.redirectUri,
        response_type: 'code',
        scope: authConfig.scope,
        code_challenge: challenge,
        code_challenge_method: 'S256',
      });
      globalThis.location.href = `${authConfig.authUrl}/oauth/authorize?${params.toString()}`;
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not start login');
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center p-8">
      <h1 className="text-2xl font-semibold">Sign in to Geek SEO</h1>
      <p className="mt-2 text-sm text-zinc-600">
        Uses Geek identity (OAuth 2.1 + PKCE). You will be redirected to the authorization server.
      </p>
      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}
      <button
        type="button"
        disabled={loading}
        onClick={() => void startLogin()}
        className="mt-8 rounded bg-zinc-900 px-4 py-3 text-white hover:bg-zinc-800 disabled:opacity-60"
      >
        {loading ? 'Redirecting…' : 'Continue with Geek account'}
      </button>
    </main>
  );
}
