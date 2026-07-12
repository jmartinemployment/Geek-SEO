/** Matches Content Writer / geekatyourspot section image path convention. */
export function buildFigureSavePath(geekApiSlug: string, headingSlug: string): string {
  const parts = geekApiSlug.trim().replace(/^\/+|\/+$/g, "").split("/").filter(Boolean);
  if (parts.length < 3) {
    throw new Error(`GeekApiSlug must be {prefix}/{department}/{pageSlug}: ${geekApiSlug}`);
  }

  const prefix = parts[0].toLowerCase();
  const department = parts[1];
  const pageSlug = parts.slice(2).join("/");

  const folder =
    prefix === "use-cases"
      ? "TechnicalArticle"
      : prefix === "blog"
        ? "Blog"
        : prefix === "tools"
          ? "Tool"
          : null;

  if (!folder) {
    throw new Error(`Unsupported GeekApiSlug prefix: ${prefix}`);
  }

  return `images/${folder}/${department}/${pageSlug}/h2-${headingSlug.trim()}.avif`;
}

export function figureSavePathDisplay(geekApiSlug: string, headingSlug: string): string {
  return `public/${buildFigureSavePath(geekApiSlug, headingSlug)}`;
}
