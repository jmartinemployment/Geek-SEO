# Content Writer V3 — Phase 5: Publication

## Problem Phase 5 Solves

**V2 Issue:** No structured publication workflow. Content either published manually or via brittle automation. No tracking of what went live, when, or to where.

**V3 Fix:** Structured publication with:
- Multiple target support (WordPress, custom CMS, Supabase, etc.)
- Scheduled publishing
- Audit trail of every publication event
- Retry logic for failures
- SEO metadata handling

---

## Phase 5 Architecture

### Entities

**Publication**
- AssetVersionId → which draft
- ReviewId → what review approved it
- Status: Queued → Scheduled → Publishing → Published → Failed
- PublishedUrl: Where it lives (for cross-linking)
- PublicationTarget: "WordPress" | "CustomCMS" | "Supabase"
- PublicationMetadata: JSON with platform-specific IDs (WordPress post_id, etc.)
- ScheduledPublishAt: For delayed publishing
- RetryCount: How many times we've tried

**PublicationEvent**
- PublicationId → parent publication
- EventType: PublishedSuccessfully | PublishFailed | ScheduleChanged | etc.
- TriggeredByUserId: Who triggered it
- Details: JSON with full context (error message if failed, metadata if published)
- OccurredAt: When

### Workflow

```
Review Approved
    ↓
[Publication Created, Status=Queued]
    ↓
Option 1: Publish Immediately
  - Content pushed to live site
  - PublicationEvent: PublishedSuccessfully
  - PublishedUrl recorded
    ↓
    [Status=Published]

Option 2: Schedule for Later
  - Set ScheduledPublishAt
  - Status=Scheduled
  - ScheduledPublishJob checks at scheduled time
    ↓
    [Publishes]
    ↓
    [Status=Published]

Failure Handling:
  - PublishJob fails → Status=Failed
  - PublicationEvent: PublishFailed with error
  - RetryCount incremented
  - Operator can retry with POST /publications/{id}/retry
```

### Publication Services

**IPublicationService**
- QueueForPublishing(reviewId, targetPlatform) → Publication entity
- PublishNow(publicationId) → tries immediate publish
- ScheduleForPublishing(publicationId, publishAt) → sets schedule
- Retry(publicationId) → retry failed publication

**IPublishAdapter** (platform-specific)
- PublishAsync(content, metadata) → returns { url, platformId, metadata }
- UnpublishAsync(platformId) → removes from live
- UpdateAsync(platformId, content) → update existing

Implementations:
- WordPressAdapter: Uses WordPress REST API + plugin
- CustomCMSAdapter: Internal API
- SupabaseAdapter: Supabase storage + webhook to frontend

### Job: PublishJob

- Type: "Publish"
- Payload: { PublicationId }
- Flow:
  1. Load Publication + Content
  2. Call IPublishAdapter for target platform
  3. On success: MarkPublished(url, metadata)
  4. On failure: MarkFailed(reason), log retry-able error
  5. If retriable && retry_count < max_retries: requeue
  6. If not retriable: mark failed, notify operator

### API Endpoints

**POST /api/content-writer/v3/publications/queue**
- Queue an approved review for publishing
- Params: reviewId, publicationTarget, scheduledAt (optional)
- Returns: PublicationId

**GET /api/content-writer/v3/publications/{publicationId}**
- Full publication details + event log

**POST /api/content-writer/v3/publications/{publicationId}/publish-now**
- Publish immediately (vs. scheduled)

**POST /api/content-writer/v3/publications/{publicationId}/schedule**
- Change schedule time
- Params: scheduledAt

**POST /api/content-writer/v3/publications/{publicationId}/retry**
- Retry a failed publication

**POST /api/content-writer/v3/publications/{publicationId}/unpublish**
- Remove from live site (marks as Unpublished)
- Creates PublicationEvent: UnpublishedSuccessfully

### Exit Criteria

- ✓ Publication queued with target platform specified
- ✓ Content pushed to live site successfully
- ✓ PublishedUrl recorded
- ✓ Audit trail captured in PublicationEvents
- ✓ SEO metadata (canonical, etc.) configured
- ✓ Ready for Phase 6 (Performance Feedback)

### Future: Cross-Linking

Phase 6 will use PublishedUrl to:
- Update internal links in related content
- Add "See also" sections pointing to this new content
- Track which content mentions which topics
