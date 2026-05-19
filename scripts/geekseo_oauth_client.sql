-- Geek SEO OAuth client for auth.oauth_clients (GeekBackend / geek-OAuth DB adapter).
-- geek-OAuth also registers client_id=geekseo in src/oidc/clients.ts — keep both in sync.
-- Run: psql "$DATABASE_URL" -f scripts/geekseo_oauth_client.sql

INSERT INTO auth.oauth_clients (
  id,
  client_id,
  client_name,
  client_secret,
  redirect_uris,
  grant_types,
  token_endpoint_auth_method,
  created_at,
  updated_at
)
VALUES (
  gen_random_uuid(),
  'geekseo',
  'Geek SEO',
  '',
  'https://seo.geekatyourspot.com/auth/callback,http://localhost:3000/auth/callback',
  'authorization_code,refresh_token',
  'none',
  now(),
  now()
)
ON CONFLICT DO NOTHING;

-- If client_id has a unique index, use upsert instead:
-- ON CONFLICT (client_id) DO UPDATE SET ...
