import { redirect } from 'next/navigation';

/** No intermediate screen — send users straight to GeekOAuth sign-in. */
export default function LoginPage() {
  redirect('/api/auth/start');
}
