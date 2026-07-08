# Cold Outreach Email + Sidebar Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Cold Outreach email generation (subject + 50–125 word body + CTA to pillar URL) as Step 5, stub other email length targets/enums, and remove the Geek-SEO app sidebar on seo.geekatyourspot.com (including `/content-writer`).

**Architecture:** Mirror social generation: new `EmailColdOutreach` content type, one LLM JSON call, persist on existing `GeneratedContent` (`Title`=subject, `BodyHtml`=body, `MetaDescription`=ctaLabel, `RelatedArticleUrl`=pillar URL). Frontend gains Step 5 + results tab. Independently, `AppShell` stops wrapping children in `SidebarLayout` so tool pages are full-width under the existing navbar/footer.

**Tech Stack:** .NET 10 (Content Writer API), Next.js (Content Writer UI + Geek-SEO shell), SQLite/Postgres via existing EF (enum values only — no migration).

**Spec:** `docs/superpowers/specs/2026-07-08-cold-outreach-email-design.md`

**Repos / sync notes:**
- Cold outreach code lives in this workspace: `/Users/jeffmartin/Documents/content-writer/`
- Live shell + Content Writer UI route live in Geek-SEO: `/Users/jeffmartin/Documents/Geek-SEO/`
- After UI edits here, mirror the same files into `Geek-SEO/frontend/src/components/content-writer/` and `Geek-SEO/frontend/src/lib/content-writer/`
- After backend edits here, mirror into `Geek-SEO/content-writer/backend/` (or edit the Geek-SEO nested copy if that is what Railway builds)
- This standalone `content-writer` folder is not a git repo; commit sidebar + mirrored CW changes in **Geek-SEO**
- Commits below apply when working inside Geek-SEO; skip `git commit` in the standalone copy

---

## File structure

| File | Responsibility |
|---|---|
| `backend/src/ContentWriter.Domain/Enums/ContentEnums.cs` | Add email enum values |
| `backend/src/ContentWriter.Application/Services/ContentLengthTargets.cs` | Email length constants + definitions |
| `backend/src/ContentWriter.Application/DTOs/GenerationRequest.cs` | `ColdOutreachEmailDraft` + extend `GeneratedContentSet` |
| `backend/src/ContentWriter.Application/Services/LlmResponseJsonParser.cs` | Parse `{ subject, bodyText, ctaLabel }` |
| `backend/src/ContentWriter.Application/Services/PromptBuilders/ContentPromptBuilder.cs` | Cold outreach prompt |
| `backend/src/ContentWriter.Application/Services/IContentGenerationOrchestrator.cs` | New method signature |
| `backend/src/ContentWriter.Application/Services/ContentGenerationOrchestrator.cs` | `GenerateColdOutreachAsync` + wire `GenerateAllAsync` |
| `backend/src/ContentWriter.Application/Services/GeneratedContentSetAssembler.cs` | Map cold outreach row → DTO |
| `backend/src/ContentWriter.Api/Controllers/GenerateController.cs` | `POST .../email-cold-outreach` |
| `backend/.../ContentWriter.Application.Tests/` (new) | Parser + word-count validation tests |
| `frontend/src/lib/content-writer/types.ts` | Types + length targets |
| `frontend/src/lib/content-writer/api.ts` | `generateColdOutreachContent` |
| `frontend/src/components/content-writer/ContentResults.tsx` | Step 5 + tab + view |
| `Geek-SEO/frontend/src/components/app/app-shell.tsx` | Remove `SidebarLayout` wrapper |
| (optional cleanup) `Geek-SEO/frontend/src/components/app/app-sidebar.tsx`, `sidebar-navigation.ts` | Leave in place unused unless deleting dead code in a follow-up |

---

### Task 1: Remove Geek-SEO sidebar from AppShell

**Files:**
- Modify: `/Users/jeffmartin/Documents/Geek-SEO/frontend/src/components/app/app-shell.tsx`
- Mirror plan copy: also sync this plan under `Geek-SEO/docs/superpowers/plans/` when committing

- [ ] **Step 1: Remove `SidebarLayout` so main content is full-width**

Replace `app-shell.tsx` with:

```tsx
'use client';

import { SiteFooter } from '@/components/layout/site-footer';
import { SiteNavbar } from '@/components/layout/site-navbar';
import { cn } from '@/lib/utils';

export function AppShell({
  children,
  mainClassName,
}: {
  children: React.ReactNode;
  mainClassName?: string;
}) {
  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-bg)]">
      <SiteNavbar />
      <main className={cn('flex-1 px-4 py-8 md:px-10', mainClassName)}>{children}</main>
      <SiteFooter />
    </div>
  );
}
```

- [ ] **Step 2: Verify locally**

Run (from Geek-SEO frontend):

```bash
cd /Users/jeffmartin/Documents/Geek-SEO/frontend && npm run lint -- --file src/components/app/app-shell.tsx
```

Expected: no errors (or project’s usual lint pass). Manually open `/` and `/content-writer` — no left sidebar rail; navbar + footer still present.

- [ ] **Step 3: Commit in Geek-SEO**

```bash
cd /Users/jeffmartin/Documents/Geek-SEO
git add frontend/src/components/app/app-shell.tsx
git commit -m "$(cat <<'EOF'
Remove Geek-SEO app sidebar from AppShell.

Tool pages including /content-writer are full-width under the site navbar.
EOF
)"
```

---

### Task 2: Domain enum + length targets

**Files:**
- Modify: `backend/src/ContentWriter.Domain/Enums/ContentEnums.cs`
- Modify: `backend/src/ContentWriter.Application/Services/ContentLengthTargets.cs`
- Modify: `frontend/src/lib/content-writer/types.ts`

- [ ] **Step 1: Extend `GeneratedContentType`**

In `ContentEnums.cs`, replace the enum with:

```csharp
public enum GeneratedContentType
{
    TechnicalArticle = 0,
    BlogPost = 1,
    SocialFacebook = 2,
    SocialLinkedIn = 3,
    EmailColdOutreach = 4,
    EmailNewsletter = 5,      // stub
    EmailStoryNurture = 6,     // stub
    EmailTransactional = 7     // stub
}
```

- [ ] **Step 2: Add email length targets**

Append to `ContentLengthTargets.cs`:

```csharp
    // Email — Cold Outreach / Sales (implemented).
    public const int EmailColdOutreachMinWords = 50;
    public const int EmailColdOutreachMaxWords = 125;

    public const string EmailColdOutreachEditorialDefinition =
        "Cold outreach and sales emails aim for high response rates with a single, clear call-to-action.";

    // Email stubs (constants only — no generation yet).
    public const int EmailNewsletterMinWords = 200;
    public const int EmailNewsletterMaxWords = 400;

    public const string EmailNewsletterEditorialDefinition =
        "Curated newsletters summarize external links and drive traffic back to the website.";

    public const int EmailStoryNurtureMinWords = 500;
    public const int EmailStoryNurtureMaxWords = 1_000;

    public const string EmailStoryNurtureEditorialDefinition =
        "Story-based nurture emails build deep trust and treat email like an exclusive blog post.";

    public const int EmailTransactionalMinWords = 1;
    public const int EmailTransactionalMaxWords = 49;

    public const string EmailTransactionalEditorialDefinition =
        "Transactional emails deliver critical data; highly functional with zero fluff.";

    public static string EmailColdOutreachRangeLabel =>
        $"{EmailColdOutreachMinWords}–{EmailColdOutreachMaxWords}";
```

- [ ] **Step 3: Mirror length targets on the frontend**

In `frontend/src/lib/content-writer/types.ts`, extend `GeneratedContentType` and `CONTENT_LENGTH_TARGETS`:

```ts
export type GeneratedContentType =
  | "TechnicalArticle"
  | "BlogPost"
  | "SocialFacebook"
  | "SocialLinkedIn"
  | "EmailColdOutreach"
  | "EmailNewsletter"
  | "EmailStoryNurture"
  | "EmailTransactional";

export interface ColdOutreachEmailDraft {
  subject: string;
  bodyText: string;
  ctaLabel: string;
  ctaUrl: string;
}

// Inside CONTENT_LENGTH_TARGETS, add:
  emailColdOutreach: {
    min: 50,
    max: 125,
    label: "50–125",
    definition:
      "High response rates; pitch a single, clear call-to-action.",
  },
  emailNewsletter: {
    min: 200,
    max: 400,
    label: "200–400",
    definition: "Summarize external links; drive traffic back to your website.",
  },
  emailStoryNurture: {
    min: 500,
    max: 1000,
    label: "500–1,000",
    definition: "Build deep trust; treats email like an exclusive blog post.",
  },
  emailTransactional: {
    min: 1,
    max: 49,
    label: "Under 50",
    definition: "Deliver critical data; highly functional with zero fluff.",
  },
```

Also add to `GeneratedContentSet`:

```ts
  coldOutreachEmail: ColdOutreachEmailDraft | null;
```

- [ ] **Step 4: Build domain/application (smoke)**

```bash
cd /Users/jeffmartin/Documents/content-writer/backend
dotnet build src/ContentWriter.Application/ContentWriter.Application.csproj
```

Expected: Build succeeded.

- [ ] **Step 5: Commit (Geek-SEO nested copy when syncing)**

```bash
# After mirroring into Geek-SEO/content-writer + Geek-SEO/frontend lib types:
cd /Users/jeffmartin/Documents/Geek-SEO
git add content-writer/backend/src/ContentWriter.Domain/Enums/ContentEnums.cs \
  content-writer/backend/src/ContentWriter.Application/Services/ContentLengthTargets.cs \
  frontend/src/lib/content-writer/types.ts
git commit -m "$(cat <<'EOF'
Add email content types and cold-outreach length targets.

Stub newsletter, story-nurture, and transactional enums/constants for later.
EOF
)"
```

---

### Task 3: Parser for cold outreach JSON (TDD)

**Files:**
- Create: `backend/tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj`
- Create: `backend/tests/ContentWriter.Application.Tests/LlmResponseJsonParserColdOutreachTests.cs`
- Modify: `backend/src/ContentWriter.Application/Services/LlmResponseJsonParser.cs`
- Modify: `backend/src/ContentWriter.Application/DTOs/GenerationRequest.cs` (draft record used by parser return)

- [ ] **Step 1: Create test project**

```bash
cd /Users/jeffmartin/Documents/content-writer/backend
mkdir -p tests/ContentWriter.Application.Tests
dotnet new xunit -n ContentWriter.Application.Tests -o tests/ContentWriter.Application.Tests --force
dotnet add tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj reference src/ContentWriter.Application/ContentWriter.Application.csproj
```

If the solution file exists, also:

```bash
dotnet sln ContentWriter.slnx add tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj
```

(Use the actual `.sln` / `.slnx` path in this repo.)

- [ ] **Step 2: Write failing tests**

Create `LlmResponseJsonParserColdOutreachTests.cs`:

```csharp
using ContentWriter.Application.Services;
using ContentWriter.Application.Providers;
using Xunit;

namespace ContentWriter.Application.Tests;

public class LlmResponseJsonParserColdOutreachTests
{
    [Fact]
    public void ParseColdOutreach_ValidJson_ReturnsDraft()
    {
        var raw = """{"subject":"Quick idea on AI prospecting","bodyText":"Hi Alex — noticed your team is scaling outbound. Most B2B teams lose hours stitching signals. We published a practical pillar on AI for prospecting and lead intelligence that shows a clean stack and first pilot. Worth a skim when you have two minutes.","ctaLabel":"Read the pillar"}""";

        var draft = LlmResponseJsonParser.ParseColdOutreach(raw, "cold outreach");

        Assert.Equal("Quick idea on AI prospecting", draft.Subject);
        Assert.Equal("Read the pillar", draft.CtaLabel);
        Assert.False(string.IsNullOrWhiteSpace(draft.BodyText));
        var words = draft.BodyText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.InRange(words, 50, 125);
    }

    [Fact]
    public void ParseColdOutreach_MissingSubject_Throws()
    {
        var raw = """{"subject":"","bodyText":"word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word word","ctaLabel":"Read more"}""";

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseColdOutreach(raw, "cold outreach"));
    }
}
```

- [ ] **Step 3: Run tests — expect fail**

```bash
cd /Users/jeffmartin/Documents/content-writer/backend
dotnet test tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj --filter ParseColdOutreach
```

Expected: FAIL (method missing).

- [ ] **Step 4: Add DTO + parser**

In `GenerationRequest.cs` add:

```csharp
public record ColdOutreachEmailDraft(string Subject, string BodyText, string CtaLabel);
```

And extend `GeneratedContentSet` to include:

```csharp
public record GeneratedContentSet(
    ArticleDraft? Article,
    string? ArticleSlug,
    string? ArticleUrl,
    string? ArticleJsonLd,
    BlogDraft? Blog,
    string? BlogSlug,
    string? BlogUrl,
    string? BlogJsonLd,
    SocialPostDraft? FacebookPost,
    SocialPostDraft? LinkedInPost,
    ColdOutreachEmailDraft? ColdOutreachEmail);
```

**Important:** Update every `new GeneratedContentSet(...)` call site (assembler + any tests) to pass `ColdOutreachEmail: ...`.

In `LlmResponseJsonParser.cs` add:

```csharp
public static ColdOutreachEmailDraft ParseColdOutreach(string rawContent, string label)
{
    var cleaned = Clean(rawContent);

    foreach (var candidate in CandidateJsonStrings(cleaned))
    {
        if (TryDeserializeColdOutreach(candidate, out var draft))
        {
            return ValidateColdOutreach(draft, label);
        }
    }

    throw new ContentGenerationException(
        $"Model did not return valid JSON for {label}. First 200 chars: {rawContent[..Math.Min(200, rawContent.Length)]}");
}

private static bool TryDeserializeColdOutreach(string json, out ColdOutreachEmailDraft draft)
{
    draft = new ColdOutreachEmailDraft("", "", "");
    try
    {
        var parsed = JsonSerializer.Deserialize<ColdOutreachResponse>(json, JsonOptions);
        if (parsed is null) return false;
        draft = new ColdOutreachEmailDraft(
            (parsed.Subject ?? "").Trim(),
            (parsed.BodyText ?? "").Trim(),
            (parsed.CtaLabel ?? "").Trim());
        return true;
    }
    catch (JsonException)
    {
        return false;
    }
}

private static ColdOutreachEmailDraft ValidateColdOutreach(ColdOutreachEmailDraft draft, string label)
{
    if (string.IsNullOrWhiteSpace(draft.Subject))
        throw new ContentGenerationException($"Model returned empty subject for {label}.");
    if (string.IsNullOrWhiteSpace(draft.BodyText))
        throw new ContentGenerationException($"Model returned empty body for {label}.");
    if (string.IsNullOrWhiteSpace(draft.CtaLabel))
        throw new ContentGenerationException($"Model returned empty ctaLabel for {label}.");

    var words = draft.BodyText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    if (words < ContentLengthTargets.EmailColdOutreachMinWords || words > ContentLengthTargets.EmailColdOutreachMaxWords)
    {
        throw new ContentGenerationException(
            $"Cold outreach body must be {ContentLengthTargets.EmailColdOutreachMinWords}–{ContentLengthTargets.EmailColdOutreachMaxWords} words (got {words}).");
    }

    return draft;
}

private sealed record ColdOutreachResponse(string? Subject, string? BodyText, string? CtaLabel);
```

Ensure `using ContentWriter.Application.DTOs;` is present and JSON property names match camelCase (`Subject`/`subject` via existing `JsonOptions`).

- [ ] **Step 5: Run tests — expect pass**

```bash
dotnet test tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj --filter ParseColdOutreach
```

Expected: PASS. If the first fixture’s word count is outside 50–125, adjust `bodyText` in the test to sit comfortably mid-range (~80 words).

- [ ] **Step 6: Commit**

```bash
cd /Users/jeffmartin/Documents/Geek-SEO
git add content-writer/backend/tests content-writer/backend/src/ContentWriter.Application
git commit -m "$(cat <<'EOF'
Parse cold outreach LLM JSON with word-count validation.

Add Application.Tests coverage for subject/body/ctaLabel contract.
EOF
)"
```

---

### Task 4: Prompt + orchestrator + assembler + API

**Files:**
- Modify: `backend/src/ContentWriter.Application/Services/PromptBuilders/ContentPromptBuilder.cs`
- Modify: `backend/src/ContentWriter.Application/Services/IContentGenerationOrchestrator.cs`
- Modify: `backend/src/ContentWriter.Application/Services/ContentGenerationOrchestrator.cs`
- Modify: `backend/src/ContentWriter.Application/Services/GeneratedContentSetAssembler.cs`
- Modify: `backend/src/ContentWriter.Api/Controllers/GenerateController.cs`

- [ ] **Step 1: Add prompt builder method**

On `IContentPromptBuilder`:

```csharp
ChatCompletionRequest BuildColdOutreachPrompt(
    ProjectGenerationContext context,
    ArticleDraft sourceArticle,
    string articleUrl);
```

Implementation:

```csharp
private const string ColdOutreachJsonContract =
    "{\"subject\": string, \"bodyText\": string (50-125 words), \"ctaLabel\": string}";

public ChatCompletionRequest BuildColdOutreachPrompt(
    ProjectGenerationContext context,
    ArticleDraft sourceArticle,
    string articleUrl)
{
    var system = new StringBuilder()
        .AppendLine("You write cold outreach / sales emails for an IT consulting firm that specializes in AI implementation.")
        .AppendLine(ContentLengthTargets.EmailColdOutreachEditorialDefinition)
        .AppendLine($"Body must be {ContentLengthTargets.EmailColdOutreachMinWords}-{ContentLengthTargets.EmailColdOutreachMaxWords} words.")
        .AppendLine("Pitch ONE clear idea. No HTML. No markdown links. Do not invent URLs.")
        .AppendLine("ctaLabel is short button/link text (e.g. \"Read the full guide\"). The destination URL is injected by the app.")
        .AppendLine("Respond with ONLY a single valid JSON object — no markdown fences:")
        .AppendLine(ColdOutreachJsonContract)
        .ToString();

    var user = new StringBuilder()
        .AppendLine($"Article title: {sourceArticle.Title}")
        .AppendLine($"Article summary: {sourceArticle.MetaDescription}")
        .AppendLine($"Pillar URL (for context only — do not put in JSON): {articleUrl}")
        .AppendLine($"Target keyword: {context.TargetKeyword}")
        .AppendLine($"Site tone: {context.DetectedTone}")
        .ToString();

    return new ChatCompletionRequest(
        Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
        Temperature: 0.65,
        MaxOutputTokens: 1024);
}
```

- [ ] **Step 2: Assembler**

Update `GeneratedContentSetAssembler.Assemble`:

```csharp
var coldOutreachRow = Find(project, GeneratedContentType.EmailColdOutreach);
// ...
ColdOutreachEmail: coldOutreachRow is null
    ? null
    : new ColdOutreachEmailDraft(
        coldOutreachRow.Title,
        coldOutreachRow.BodyHtml,
        coldOutreachRow.MetaDescription ?? string.Empty)
// wait — API DTO for frontend needs ctaUrl too
```

Frontend needs `ctaUrl`. Prefer a **response DTO** that includes URL:

Change the public set field to a richer type, or map in assembler to a new record:

```csharp
public record ColdOutreachEmailContent(
    string Subject,
    string BodyText,
    string CtaLabel,
    string CtaUrl);
```

Use `ColdOutreachEmailContent` on `GeneratedContentSet` (parsed LLM draft stays `ColdOutreachEmailDraft` without URL). Assembler:

```csharp
ColdOutreachEmail: coldOutreachRow is null
    ? null
    : new ColdOutreachEmailContent(
        coldOutreachRow.Title,
        coldOutreachRow.BodyHtml,
        coldOutreachRow.MetaDescription ?? string.Empty,
        coldOutreachRow.RelatedArticleUrl ?? articleUrl ?? string.Empty)
```

- [ ] **Step 3: Orchestrator method**

On `IContentGenerationOrchestrator`:

```csharp
Task<GeneratedContentSet> GenerateColdOutreachAsync(Guid projectId, CancellationToken cancellationToken = default);
```

In `ContentGenerationOrchestrator`:

```csharp
public async Task<GeneratedContentSet> GenerateColdOutreachAsync(Guid projectId, CancellationToken cancellationToken = default)
{
    var project = await LoadProjectForGenerationAsync(projectId, cancellationToken);
    var provider = _providerFactory.GetProvider(project.PreferredProvider);
    var context = BuildContext(project);
    var articleRow = RequireCompletePillar(project);
    var article = GeneratedContentSetAssembler.ToArticleDraft(articleRow);
    var articleUrl = CombineUrl(context.ArticleBaseUrl, articleRow.Slug);

    _logger.LogInformation("Generating cold outreach email for project {ProjectId} via {Provider}", projectId, provider.ProviderType);

    RemoveGeneratedContents(project, GeneratedContentType.EmailColdOutreach);

    const int maxAttempts = 2;
    ColdOutreachEmailDraft draft = null!;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        var result = await provider.CompleteAsync(
            _promptBuilder.BuildColdOutreachPrompt(context, article, articleUrl),
            cancellationToken);
        try
        {
            draft = LlmResponseJsonParser.ParseColdOutreach(result.Content, "cold outreach email");
            break;
        }
        catch (ContentGenerationException ex) when (attempt < maxAttempts)
        {
            _logger.LogWarning(ex, "Retrying cold outreach after invalid JSON (attempt {Attempt})", attempt);
        }
    }

    if (draft is null)
        throw new ContentGenerationException("Model did not return valid JSON for cold outreach email after 2 attempts.");

    var wordCount = draft.BodyText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    await AddContentAsync(project, provider.ProviderType, new GeneratedContent
    {
        ProjectId = project.Id,
        ContentType = GeneratedContentType.EmailColdOutreach,
        Title = draft.Subject,
        Slug = $"{articleRow.Slug}-cold-outreach",
        BodyHtml = draft.BodyText,
        MetaDescription = draft.CtaLabel,
        RelatedArticleUrl = articleUrl,
        WordCount = wordCount,
        GeneratedByProvider = provider.ProviderType,
        GeneratedByModel = ResolveModelName(project.PreferredProvider)
    }, cancellationToken);

    await SaveProjectAsync(project, ProjectStatus.Completed, cancellationToken);
    return Assemble(project);
}
```

Match existing private helper names (`BuildContext`, `CombineUrl`, `AddContentAsync`, `SaveProjectAsync`) exactly as already used for social — do not invent new names.

Update `GenerateAllAsync`:

```csharp
public async Task<GeneratedContentSet> GenerateAllAsync(Guid projectId, CancellationToken cancellationToken = default)
{
    await GeneratePillarPlanAsync(projectId, cancellationToken);
    await GeneratePillarBodyAsync(projectId, cancellationToken);
    await GenerateBlogAsync(projectId, cancellationToken);
    await GenerateSocialAsync(projectId, cancellationToken);
    return await GenerateColdOutreachAsync(projectId, cancellationToken);
}
```

- [ ] **Step 4: API endpoint**

In `GenerateController.cs`:

```csharp
[HttpPost("email-cold-outreach")]
public Task<IActionResult> GenerateColdOutreach(Guid projectId, CancellationToken cancellationToken) =>
    RunStep(projectId, _orchestrator.GenerateColdOutreachAsync(projectId, cancellationToken), "email-cold-outreach");
```

- [ ] **Step 5: Build + test**

```bash
cd /Users/jeffmartin/Documents/content-writer/backend
dotnet build
dotnet test tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj
```

Expected: Build succeeded; tests PASS.

- [ ] **Step 6: Commit**

```bash
cd /Users/jeffmartin/Documents/Geek-SEO
git add content-writer/backend
git commit -m "$(cat <<'EOF'
Generate cold outreach email from the pillar article.

Adds prompt, orchestrator step, assembler mapping, and generate API route.
EOF
)"
```

---

### Task 5: Frontend API + ContentResults UI

**Files:**
- Modify: `frontend/src/lib/content-writer/api.ts`
- Modify: `frontend/src/lib/content-writer/types.ts` (if `ColdOutreachEmailDraft` / set fields not finished in Task 2)
- Modify: `frontend/src/components/content-writer/ContentResults.tsx`
- Mirror into: `Geek-SEO/frontend/src/lib/content-writer/*` and `Geek-SEO/frontend/src/components/content-writer/ContentResults.tsx`

- [ ] **Step 1: API client**

```ts
export function generateColdOutreachContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(
    `/api/projects/${projectId}/generate/email-cold-outreach`,
    { method: "POST" },
  );
}
```

- [ ] **Step 2: Wire Step 5 + tab in `ContentResults.tsx`**

Key changes (integrate into existing file — do not replace unrelated steps):

```ts
import {
  generateBlogContent,
  generateColdOutreachContent,
  generatePillarBodyContent,
  generatePillarPlanContent,
  generateSocialContent,
  ApiError,
} from "@/lib/content-writer/api";

type Tab = "article" | "blog" | "facebook" | "linkedin" | "cold-outreach";
type GeneratingStep = "pillar-plan" | "pillar-body" | "blog" | "social" | "cold-outreach" | "all" | null;

const TABS: { id: Tab; label: string }[] = [
  { id: "article", label: "Technical Article" },
  { id: "blog", label: "Blog Post" },
  { id: "facebook", label: "Facebook" },
  { id: "linkedin", label: "LinkedIn" },
  { id: "cold-outreach", label: "Cold Outreach" },
];
```

After the Social `StepRow`, add Step 5 (last):

```tsx
        <StepRow
          step={5}
          title="Cold outreach email"
          description={`${CONTENT_LENGTH_TARGETS.emailColdOutreach.definition} Target ${CONTENT_LENGTH_TARGETS.emailColdOutreach.label} words — subject, body, and one CTA to the pillar.`}
          done={result?.coldOutreachEmail != null}
          disabled={!hasPillarBody || isGenerating}
          isRunning={generatingStep === "cold-outreach"}
          buttonLabel={result?.coldOutreachEmail ? "Regenerate email" : "Generate email"}
          onClick={() => runStep("cold-outreach", () => generateColdOutreachContent(projectId))}
          lockedMessage={!hasPillarBody ? "Complete Step 2 first." : undefined}
        />
```

Update “Generate all remaining” to also call cold outreach after social when missing, and disable when `hasPillarBody && hasBlog && hasSocial && result?.coldOutreachEmail`.

Tab disabled rule for cold outreach: `!result.coldOutreachEmail`.

Render:

```tsx
{activeTab === "cold-outreach" && result.coldOutreachEmail && (
  <ColdOutreachView email={result.coldOutreachEmail} />
)}
```

Add helper component:

```tsx
function ColdOutreachView({
  email,
}: {
  email: { subject: string; bodyText: string; ctaLabel: string; ctaUrl: string };
}) {
  const words = countWords(email.bodyText);
  const under =
    words < CONTENT_LENGTH_TARGETS.emailColdOutreach.min ||
    words > CONTENT_LENGTH_TARGETS.emailColdOutreach.max;

  return (
    <div>
      <div className="mb-2 flex flex-wrap gap-2 text-xs text-muted">
        <span
          className={`rounded-full px-2 py-0.5 font-medium ${
            under ? "bg-amber-100 text-amber-800" : "bg-brand/10 text-brand"
          }`}
        >
          {words} words
        </span>
        <span>Target: {CONTENT_LENGTH_TARGETS.emailColdOutreach.label} words</span>
      </div>
      <h3 className="text-lg font-semibold text-foreground">{email.subject}</h3>
      <div className="mt-3 whitespace-pre-wrap rounded-lg border border-border bg-background p-4 text-sm text-foreground">
        {email.bodyText}
      </div>
      <p className="mt-3 text-sm text-foreground">
        <span className="font-medium">{email.ctaLabel}: </span>
        <a href={email.ctaUrl} className="text-brand hover:underline" target="_blank" rel="noreferrer">
          {email.ctaUrl}
        </a>
      </p>
    </div>
  );
}
```

Update intro copy on the page if it still says only article/blog/social — optional one-line mention of cold outreach email.

- [ ] **Step 3: Mirror into Geek-SEO frontend**

```bash
cp /Users/jeffmartin/Documents/content-writer/frontend/src/lib/content-writer/types.ts \
  /Users/jeffmartin/Documents/Geek-SEO/frontend/src/lib/content-writer/types.ts
cp /Users/jeffmartin/Documents/content-writer/frontend/src/lib/content-writer/api.ts \
  /Users/jeffmartin/Documents/Geek-SEO/frontend/src/lib/content-writer/api.ts
cp /Users/jeffmartin/Documents/content-writer/frontend/src/components/content-writer/ContentResults.tsx \
  /Users/jeffmartin/Documents/Geek-SEO/frontend/src/components/content-writer/ContentResults.tsx
```

If Geek-SEO `api.ts` differs (SEO base URL), merge carefully — only add the new function + keep Geek-SEO’s API_BASE_URL pattern.

- [ ] **Step 4: Manual UI check**

With API running and a project that already has a pillar body:

1. Open `/content-writer`
2. Confirm no sidebar (Task 1)
3. Step 5 enabled; Generate email
4. Cold Outreach tab shows subject, body, CTA + pillar URL
5. Regenerate replaces content

- [ ] **Step 5: Commit**

```bash
cd /Users/jeffmartin/Documents/Geek-SEO
git add frontend/src/lib/content-writer frontend/src/components/content-writer/ContentResults.tsx \
  content-writer/backend
git commit -m "$(cat <<'EOF'
Add cold outreach email Step 5 and results tab.

Unlocks after pillar body; CTA always links to the pillar article.
EOF
)"
```

---

### Task 6: Spec / plan sync + README touch (optional, small)

**Files:**
- Modify: `README.md` (content-writer) — mention cold outreach in the product blurb
- Copy plan/spec into Geek-SEO docs if desired

- [ ] **Step 1: Update README product sentence**

Change the opening blurb to include cold outreach email in the output list.

- [ ] **Step 2: Final verification**

```bash
cd /Users/jeffmartin/Documents/content-writer/backend && dotnet test tests/ContentWriter.Application.Tests/ContentWriter.Application.Tests.csproj
cd /Users/jeffmartin/Documents/Geek-SEO/frontend && npx tsc --noEmit
```

Expected: tests PASS; `tsc` clean (or no new errors from these files).

- [ ] **Step 3: Commit docs**

```bash
cd /Users/jeffmartin/Documents/Geek-SEO
git add docs content-writer/README.md README.md 2>/dev/null || true
git commit -m "$(cat <<'EOF'
Document cold outreach email output and sidebar removal.
EOF
)"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|---|---|
| `EmailColdOutreach` + stub enums | Task 2 |
| Length targets all four email types | Task 2 |
| Storage: Title/Body/MetaDescription/RelatedArticleUrl | Task 4 |
| JSON contract subject/bodyText/ctaLabel | Task 3–4 |
| Pillar unlock gate | Task 4–5 |
| Step 5 button last | Task 5 |
| Results tab last | Task 5 |
| Generate all includes email after social | Task 4–5 |
| Out of scope (ESP, HTML templates, other email types) | Not implemented |
| Sidebar removed on seo.geekatyourspot.com + `/content-writer` | Task 1 |

## Self-review notes

- No placeholders left; CTA URL is orchestrator-injected (not model-invented).
- `GeneratedContentSet` gains a trailing field — every constructor call must be updated.
- Sidebar files (`app-sidebar.tsx`, `sidebar-navigation.ts`) intentionally left unused for now (YAGNI delete); can remove in a dead-code cleanup later.
- Prefer implementing backend in this workspace then mirroring into `Geek-SEO/content-writer` so Railway builds pick up changes.
