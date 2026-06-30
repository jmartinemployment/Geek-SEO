export function usesAppShell(pathname: string): boolean {
  if (pathname.startsWith('/auth')) {
    return false;
  }

  return (
    pathname === '/' ||
    pathname.startsWith('/analytics') ||
    pathname.startsWith('/audit') ||
    pathname.startsWith('/brand-voice') ||
    pathname.startsWith('/briefs') ||
    pathname.startsWith('/bulk') ||
    pathname.startsWith('/calendar') ||
    pathname.startsWith('/cannibalization') ||
    pathname.startsWith('/content-guard') ||
    pathname.startsWith('/content-writing') ||
    pathname.startsWith('/dashboard') ||
    pathname.startsWith('/geo') ||
    pathname.startsWith('/guided') ||
    pathname.startsWith('/keywords') ||
    pathname.startsWith('/planner') ||
    pathname.startsWith('/projects') ||
    pathname.startsWith('/rankings') ||
    pathname.startsWith('/serp') ||
    pathname.startsWith('/settings') ||
    pathname.startsWith('/site-analyzer') ||
    pathname.startsWith('/strategy') ||
    pathname.startsWith('/url-analyzer')
  );
}

export function appShellMainClassName(pathname: string): string | undefined {
  if (pathname.startsWith('/content-writing') || pathname.startsWith('/site-analyzer')) {
    return 'px-2 py-4 sm:px-4 lg:px-6';
  }
  return undefined;
}
