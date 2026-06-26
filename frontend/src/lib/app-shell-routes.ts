export function usesAppShell(pathname: string): boolean {
  if (pathname.startsWith('/auth')) {
    return false;
  }

  return (
    pathname === '/' ||
    pathname.startsWith('/app') ||
    pathname.startsWith('/site-analyzer') ||
    pathname.startsWith('/content-writing') ||
    pathname.startsWith('/url-analyzer') ||
    pathname.startsWith('/projects/')
  );
}

export function appShellMainClassName(pathname: string): string | undefined {
  if (pathname.startsWith('/content-writing')) {
    return 'px-2 py-4 sm:px-4 lg:px-6';
  }
  return undefined;
}
