# Mobile-First Crawl: Single Canonical Html, Regex-Free Heading Tree

## Context

Site Structure was showing duplicate headings (H1/H2 repeated 2-3x on a page). Root cause: the
crawler (`SitePageCrawler.FetchWithPlaywrightAsync`) captures the desktop-rendered DOM (Chromium's
default 1280×720 viewport — never explicitly set) via `page.ContentAsync()`. Responsive
Tailwind/Next.js markup commonly renders two literal copies of a section (one `md:hidden` for
mobile, one `hidden md:block` for desktop) — both exist in the DOM at once; CSS just toggles which
one is visually shown. The regex-based tree builder has no concept of computed CSS visibility, so
it captures every heading match verbatim, duplicates included.

A dedup filter existed for this before (`IsDuplicateHeading`) but was explicitly rejected as the
fix this session — the user does not want text-based sibling dedup in any form. Instead, the
decided direction is architectural: **stop capturing the desktop DOM at all.** The user's long-term
goal is building their own crawler ("my own GoogleBot"), and wants Site Analyzer's crawl to follow
Google/Bing's actual approach — mobile-first indexing, rendering at a mobile viewport, only
capturing what's actually visible there. Under this approach, duplicates from responsive
desktop/mobile variants stop occurring by construction: the desktop-only copy never computes to
visible at a mobile viewport, so there's nothing to dedupe.

**Explicitly accepted tradeoff:** this makes the crawl intentionally incomplete relative to today's
desktop-biased behavior (content that's mobile-hidden, or behind a click-to-expand widget Google
doesn't interact with, won't be captured) — user confirmed this is fine and matches how Google
itself sees these pages. Current work is Home-page-focused, which mostly already has a defined
mobile layout, so this is not expected to lose much in practice right now.

**Scope, confirmed by user:** mobile-first + visibility-filtered HTML becomes the single canonical
`Html` for the *entire* crawl pipeline, not a special case for heading extraction. Confirmed
consumers, all reading the same shared value, all through `SitePageCrawler`:

1. `PageSectionTreeBuilder.Build(page.Html)` — heading/paragraph tree (original bug report)
   — called from `SiteAnalysisStepExecutionService.cs:301`.
2. `SitePageCrawler.ExtractSameOriginLinks(html, url, origin)` — BFS link discovery queue,
   internal to `SitePageCrawler.cs`.
3. `IsSoft404(html, url)` — soft-404 detection, internal to `SitePageCrawler.cs`.
4. `SchemaOrgExtractor.ParseFromHtml(page.Html)` — competitor-page schema extraction, called from
   `CompetitorPageFetcher.cs:58` (which reuses `SitePageCrawler` for competitor pages).

No new field, no dual `Html`/`VisibleHtml` split — `FetchWithPlaywrightAsync`'s return value changes
in place, and every consumer above inherits the new mobile-first behavior automatically since they
all read the same `CrawledPage.Html`.

**Separately flagged, explicitly out of scope for this plan:** `SchemaOrgExtractor.ExtractAsync`
(the path used for the site actually being analyzed, not competitors) does its own independent
Playwright page load of the homepage (`ExtractJsonLdWithPlaywrightAsync`) rather than reusing
`SitePageCrawler`'s already-crawled pages — a real architectural redundancy (the homepage gets
rendered twice: once by the BFS crawl, once again here) worth fixing later, but not part of this
heading-dedup/mobile-viewport fix.

## Design

### 1. Mobile viewport + identity (`SitePageCrawler.cs`, `NewContextAsync` call, ~line 67)

Neither Google nor Bing publish an exact Googlebot Smartphone viewport, but Google's own testing
tooling (Lighthouse/PageSpeed Insights — Google's standard reference for "how Google renders
mobile") emulates **412×823 at 1.75x device scale factor**, mobile + touch enabled. Use that as the
defensible "follow Google's lead" baseline:

```csharp
playwrightContext = await browser.NewContextAsync(new BrowserNewContextOptions
{
    UserAgent = "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Mobile Safari/537.36 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)",
    ViewportSize = new ViewportSize { Width = 412, Height = 823 },
    IsMobile = true,
    HasTouch = true,
    DeviceScaleFactor = 1.75f,
});
```

Keeps the existing self-identifying suffix (transparent bot identity, matching Googlebot/Bingbot
convention of self-declaring in the UA string) while presenting as a real mobile Chrome UA so
server-side UA-sniffing (rare today, but possible) also sees a mobile client, not just the CSS
media queries.

### 2. Visibility-filtered capture (`FetchWithPlaywrightAsync`, replaces `page.ContentAsync()`)

Real computed-style check inside the live Playwright page — no regex, no text matching:

```csharp
var html = await page.EvaluateAsync<string>(@"() => {
    const isHidden = (el) => {
        const style = window.getComputedStyle(el);
        return style.display === 'none' || style.visibility === 'hidden';
    };
    document.querySelectorAll('*').forEach(el => {
        if (isHidden(el)) el.remove();
    });
    return document.documentElement.outerHTML;
}");
```

Runs after the existing `page.WaitForTimeoutAsync(RenderSettleMs)` render-settle wait, in place of
`page.ContentAsync()`. Everything downstream (`IsSoft404`, `ExtractSameOriginLinks`,
`PageSectionTreeBuilder.Build`, competitor schema extraction) receives this filtered HTML
transparently — no call-site changes needed beyond this one line, since they already consume
`page.Html`/the `html` variable that this replaces.

### 3. Rewrite `PageSectionTreeBuilder.Build()` off regex (still needed, independent of the above)

Per the existing, never-executed plan `docs/plans/remove-regex-immediate.md` (status was "Planned —
immediate," still pending) — replace `NodeRegex`/`TagRegex`/`WhitespaceRegex` with `HtmlAgilityPack`
DOM traversal (already a dependency, already `using`'d in this file for last session's link-extraction
work). Walk `h1`–`h6` and paragraph nodes in document order, build the same stack-by-level tree,
`CleanText` becomes `HtmlDecode(node.InnerText).Trim()` + manual whitespace collapse (no regex
needed for that).

**No dedup logic of any kind** — with visibility-filtered mobile-only input, true responsive
duplicates won't exist in the tree to begin with. Delete the dead `IsDuplicateHeading` function
entirely rather than leaving it defined-but-unused (it's been dead code since `f302fbc`, predates
this session, and the user has explicitly ruled out ever calling it again).

## Files to change

- `Geek-SEO/GeekSeoBackend/Services/SiteExtraction/SitePageCrawler.cs` — mobile viewport/UA in
  `NewContextAsync` (~line 67); replace `page.ContentAsync()` with the visibility-filtered
  `page.EvaluateAsync<string>(...)` in `FetchWithPlaywrightAsync` (~line 270).
- `Geek-SEO/GeekSeoBackend/Services/SiteExtraction/PageSectionTreeBuilder.cs` — rewrite `Build()` to
  HtmlAgilityPack DOM walk; delete unused `IsDuplicateHeading`.
- `Geek-SEO/GeekSeoBackend.Tests/PageSectionTreeBuilderTests.cs`,
  `SiteContentTreeGapTests.cs` — verify existing expectations still hold (these pass static HTML
  strings directly to `Build()`, independent of the crawler change — h5-inside-`li` handling from
  `74e737d` must not regress).

## Out of scope (flagged, not blocking)

- `HomepageHeadingsExtractor.cs` has its own separate Playwright DOM path (already non-regex per
  `remove-regex-immediate.md`'s notes) and its own HTTP-fallback regex — not touched here; same
  mobile-viewport treatment could apply later if it turns out to matter.
- `SchemaOrgExtractor.ExtractAsync`'s redundant independent Playwright fetch of the homepage (see
  Context section above) — a real duplicate-crawl inefficiency, but a separate fix from this plan.

## Verification (Part 1)

1. `dotnet build` on Geek-SEO — 0 errors.
2. `dotnet test` — `PageSectionTreeBuilderTests`, `SiteContentTreeGapTests` pass unchanged (they
   don't depend on the crawler, only on `Build()`'s parsing logic).
3. Re-run Site Analyzer on the page from the bug report; confirm H1/H2 duplicates (Redefine Your
   Business Efficiency, The Methodology, Schedule a Free Consultation) each now appear once, with
   no dedup logic involved — because the desktop-only duplicate copies are no longer in the captured
   HTML at all.
4. Confirm h5 sequence under "AI Content Creation Workflow" (Automated Content Generation, AI
   Content Repurposing, Bulk Social Media Scheduling, SEO Blog and Article Generation, Personalized
   Email Campaigns) still lists all 5 on the Home page (which the user notes mostly has mobile
   layout already defined) — this was the case `6dd704c` protected against regressing.
5. Confirm Workflow hierarchy child-heading injection still works for a project matched to a node
   with h5 children (consumer-side check of #4).
6. Spot-check that BFS link discovery (`ExtractSameOriginLinks`) still finds a reasonable set of
   same-origin links on the mobile-rendered HTML — mobile nav (e.g. hamburger menus) usually still
   renders full link markup in the DOM, just visually hidden until toggled, so this should hold, but
   worth a real check since it's now filtered before link extraction runs.
7. `grep -R GeneratedRegex GeekSeoBackend/Services/SiteExtraction/PageSectionTreeBuilder.cs` — no
   heading/paragraph-structure regexes remain.

---

# Part 2: Fix Incomplete/Misaligned Tool Extraction

## Context

Testing the tool-linking work from the prior session (recursive descendant-paragraph fix, "Finding
C") surfaced two further bugs, found by comparing generated output against the actual homepage:

- **Missing tools:** Surfer, HubSpot, and Frase — all real tools listed somewhere on the homepage
  — never appear in `Project.HierarchyToolNames` or generated output at all.
- **Misaligned tools:** ChatGPT appears in generated prose under "Bulk Social Media Scheduling,"
  but on the homepage ChatGPT is only ever listed under a *different* h5 section ("Automated
  Content Generation" or similar) — the generated placement doesn't match the source page's actual
  association.

**Confirmed root causes (`GeekContentCreator/src/lib/content-creator/hierarchy-match.ts`):**

1. **First-match-only bug** (`parseHierarchyToolNames`, lines 80-95): the function iterates the
   subtree's paragraphs and `return`s as soon as it finds the *first* paragraph matching the
   `Top ... Tools:` pattern. If the matched hierarchy node's subtree contains multiple such
   paragraphs under different h5 headings (e.g. one under "Automated Content Generation" listing
   Jasper/Copy.ai/ChatGPT/Claude, another under "SEO Blog and Article Generation" listing
   Surfer/Frase, another under "Lead Capture Pipeline" listing HubSpot), only the first one
   encountered in document order is kept — every subsequent tool-list paragraph in the same subtree
   is silently discarded. This directly explains the missing tools.
2. **Lost heading association** (`toMatch`, `HierarchyMatch.toolNames: string[]`): even for tools
   that do get extracted, the result is one flat list with no memory of which heading each tool
   came from. `ResearchBriefBuilder.AppendKnownToolsBrief`
   (`GeekBackend/GeekAPI/Services/Workflow/Services/PromptBuilders/ResearchBriefBuilder.cs`) hands
   the LLM this flat list with "weave these in wherever contextually relevant" — there is no
   per-section grounding telling it which tool belongs to which topic, so the LLM is free to place
   a tool under any section, including ones it was never actually associated with on the source
   page. This explains the ChatGPT misplacement.

## Design

### 1. Collect all matching tool-list paragraphs, not just the first

Change `parseHierarchyToolNames` to accumulate names from *every* paragraph in the subtree that
matches the tool-list pattern, not return on the first hit. Also capture *which* paragraph/heading
each match came from (needed for #2) rather than discarding that context once names are extracted.

This is also the natural point to fold in the still-pending item from the original tool-linking
plan: replace the sentence-pattern regex (`^Top\s+.+?\s+Tools?\s*:\s*(.+)$`) with structural
anchor-density detection (a paragraph fragment containing 2+ `<a href>` elements is treated as a
tool list, harvesting each anchor's text/href directly) — addresses the standing "hate regex"
feedback and is more robust to phrasing variation (not every tool-list paragraph necessarily starts
with the literal word "Top").

### 2. Preserve tool → heading association through to the prompt

Change the shape carried from extraction through to generation from a flat tool-name list to a
per-heading grouping, e.g. `toolsByHeading: { heading: string; tools: string[] }[]` (or
`{ heading: string; tools: { name: string; href?: string }[] }[]` once combined with the
crawler-side link-preservation work from the prior session). Thread this through the same layers
`HierarchyToolNames`/`HierarchyToolUrls` already use (`Project.cs`, `ProjectContracts.cs`,
`ProjectSnapshotSerializer`, `ProjectGenerationContext`, `ContentGenerationOrchestrator.BuildContext`).

In `AppendKnownToolsBrief`, print tools grouped under their source heading instead of one flat
bullet list, and instruct the LLM to prefer placing each tool's mention in body sections that match
or relate to its associated heading — not "anywhere contextually relevant" with no anchor. For
section-scoped generation calls (`ArticleSection`, `BlogSection` — one call per section), this also
opens the door to only passing the tools actually associated with *that* section's heading, rather
than the entire project's tool set every time, which would structurally prevent the ChatGPT-style
misplacement rather than just discouraging it via prompt wording.

## Files to change

- `GeekContentCreator/src/lib/content-creator/hierarchy-match.ts` — `parseHierarchyToolNames`
  (collect all matches, not first-only; consider structural anchor-based detection per above);
  `HierarchyMatch` shape gains per-heading grouping alongside/replacing flat `toolNames`.
- `GeekContentCreator/src/components/content-writer/HierarchyContextPanel.tsx` — `persistSelection`
  already threads `toolLinks`/`toolNames` to `updateProjectHierarchyContext`; update for the new
  per-heading shape.
- `GeekContentCreator/src/services/content-writer-api.ts`, `src/lib/types.ts` — contract types for
  the new shape.
- `GeekBackend/GeekAPI/Controllers/Workflow/Contracts/ProjectContracts.cs`,
  `Services/Workflow/Domain/Entities/Project.cs`,
  `Services/Workflow/Infrastructure/Serialization/ProjectSnapshotSerializer.cs`,
  `Controllers/Workflow/ProjectsController.cs`,
  `Services/Workflow/DTOs/GenerationRequest.cs`,
  `Services/Workflow/Services/ContentGenerationOrchestrator.cs` — thread the per-heading shape
  through the same path `HierarchyToolNames`/`HierarchyToolUrls` already follow.
- `GeekBackend/GeekAPI/Services/Workflow/Services/PromptBuilders/ResearchBriefBuilder.cs` —
  `AppendKnownToolsBrief`: group tools by heading in the printed brief; optionally scope to the
  current section's heading for `ArticleSection`/`BlogSection` calls.

## Verification (Part 2)

1. `npm run build` (GeekContentCreator), `dotnet build` (GeekAPI) — 0 errors.
2. Re-apply hierarchy match on the homepage project; confirm `Project.HierarchyToolNames` (or its
   replacement) now includes Surfer, HubSpot, and Frase alongside Jasper/Copy.ai/ChatGPT/Claude —
   proving the first-match-only bug is fixed.
3. Generate pillar/blog content; confirm ChatGPT (and other tools) are mentioned only in sections
   that match their actual source heading on the homepage, not scattered arbitrarily.
4. Confirm a tool with no matching source paragraph in a given section still gets no fabricated
   mention there (no regression of the "never invent tool names" guarantee).
