/** Client hub method names — must match GeekSeoBackend.Hubs.SeoHubClientEvents. */
export const SEO_HUB_EVENTS = {
  draftJobProgress: 'DraftJobProgress',
  draftJobComplete: 'DraftJobComplete',
} as const;

/** User-targeted pushes (Clients.User) — root listener wired on every hub connect. */
export const ALWAYS_WIRED_USER_EVENTS: readonly string[] = [
  SEO_HUB_EVENTS.draftJobProgress,
  SEO_HUB_EVENTS.draftJobComplete,
];
