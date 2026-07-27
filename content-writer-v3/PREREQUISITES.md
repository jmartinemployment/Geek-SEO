# Content Writer V3: Prerequisites Before New Features

## Critical Path Blockers (Must Have Before Phases 7-14)

### 1. Real LLM Integration ⚠️ BLOCKING
**Current State**: MockContentGenerator, MockPublishAdapter  
**Needed**: Claude API for intelligent content generation

**Tasks**:
- [ ] Create `ClaudeContentGenerator` (implements IContentGenerator)
  - Takes DraftGenerationContext
  - Calls Claude API with context prompt
  - Handles streaming, retries, cost tracking
- [ ] Create content generation prompts for each section type
- [ ] Add cost tracking (token usage → billing)
- [ ] Add quality validation (Claude output meets basic standards)
- [ ] Implement graceful fallback if Claude unavailable

**Impact**: Without this, all content generated is mock. Phases 7-14 won't have real content to measure.

**Estimate**: 3-4 days

---

### 2. Analytics Integration ⚠️ BLOCKING
**Current State**: ContentPerformance schema exists, but no data collection  
**Needed**: Real Google Analytics 4 sync

**Tasks**:
- [ ] Set up Google Analytics 4 project for client
- [ ] Create `GoogleAnalyticsAdapter` (implements IAnalyticsAdapter)
  - OAuth2 flow for GA4 access
  - Query GA4 API for metrics (views, engagement, conversions, time-on-page, bounce)
  - Daily sync job pulls last 7 days of data
- [ ] Create `SyncAnalyticsJob` handler
  - Runs daily at 2am
  - Fetches metrics for published URLs
  - Updates ContentPerformance records
- [ ] Create fallback: manual metrics upload via CSV/API
- [ ] Add metrics validation (sanity checks for outliers)

**Impact**: Phase 6's feedback loop is useless without real data. Phases 11+ depend on this.

**Estimate**: 4-5 days

---

### 3. Database Migrations for Production ⚠️ BLOCKING
**Current State**: Entities defined, but no production migration script  
**Needed**: Clean EF Core migrations from Phase 0 through Phase 6

**Tasks**:
- [ ] Audit all entity changes across phases
- [ ] Create single comprehensive migration: `20260801_InitialContentWriterV3Complete`
  - All Phase 0-6 tables
  - Indexes (unique on idempotency key, performance queries)
  - Concurrency tokens on Version fields
  - Constraints and foreign keys
- [ ] Test migration on fresh PostgreSQL
- [ ] Test migration on existing database (rollback capability)
- [ ] Document rollback procedure

**Impact**: Can't deploy to staging/production without clean migrations.

**Estimate**: 2 days

---

### 4. Backend API-to-Frontend Connection ⚠️ BLOCKING
**Current State**: APIs exist, frontend uses mock data  
**Needed**: Wire frontend to real backend

**Tasks**:
- [ ] Update `lib/api.ts` with all V3 endpoints
  - Campaign CRUD
  - Research endpoints
  - Insights fetching (with ranking)
  - Strategy briefs
  - Drafts
  - Reviews
  - Publications
  - Performance analytics
- [ ] Add auth token handling to all requests
- [ ] Test CORS configuration
- [ ] Add error handling for failed requests
- [ ] Create loading/error states in all pages

**Impact**: Currently frontend is completely disconnected. Can't test UX flows.

**Estimate**: 3 days

---

### 5. Authentication & User Context ⚠️ BLOCKING
**Current State**: No auth system, no user context  
**Needed**: User identity for audit trails and permissions

**Tasks**:
- [ ] Add `User` entity
  - Id, email, name, workspace, role
- [ ] Implement auth middleware (JWT or OAuth)
  - Validate token on every API request
  - Extract userId for audit trails
- [ ] Update Job, ApprovalEvent, PublicationEvent, ReviewComment to track user
- [ ] Add authorization checks (can user review this? publish this?)
- [ ] Create login/logout flow

**Impact**: Audit trails won't work. Workflow can't track who did what.

**Estimate**: 3 days

---

### 6. Comprehensive Error Handling & Logging 🔴 HIGH PRIORITY
**Current State**: Minimal error handling in handlers  
**Needed**: Production-grade observability

**Tasks**:
- [ ] Add structured logging (Serilog to PostgreSQL)
  - Every job: start, progress, completion, failure
  - Every API call: request, response, errors
  - Every LLM call: prompt, tokens, cost, latency
- [ ] Create error codes and messages
  - Distinguish retriable vs permanent errors
  - User-facing vs operational errors
- [ ] Add exception handlers in job workers
  - Catch unhandled exceptions
  - Log with full context
  - Mark job as failed with error details
- [ ] Add monitoring alerts
  - Alert on job failure rate > 5%
  - Alert on LLM cost spike
  - Alert on analytics sync failure

**Impact**: Without logging, can't debug failures in production. Can't optimize costs.

**Estimate**: 3-4 days

---

### 7. Unit Tests for Core Services 🔴 HIGH PRIORITY
**Current State**: No tests  
**Needed**: Tests for critical business logic

**Tasks**:
- [ ] InsightExtractor
  - Mock LLM responses
  - Verify ranking logic (importance × difficulty)
  - Verify skipping logic
- [ ] ContentPlanService
  - Verify variable-length outlines
  - Verify section ordering
- [ ] ContentIntelligenceValidator
  - Verify redundancy detection
  - Verify offering alignment
- [ ] PerformanceService
  - Verify quality score calculation
  - Verify insight feedback aggregation
  - Verify retirement logic (3+ uses, <30% success)
- [ ] Job handlers
  - Mock DB context
  - Verify job status transitions
  - Verify error handling

**Impact**: Can't confidently deploy changes. Can't refactor safely.

**Estimate**: 4-5 days

---

### 8. WordPress & CMS Adapters 🟡 MEDIUM PRIORITY
**Current State**: MockPublishAdapter only  
**Needed**: Real publishing to client sites

**Tasks**:
- [ ] WordPress REST API adapter
  - Authentication (app password)
  - Create/update/delete posts
  - Set featured image
  - Add post meta (canonical URL, etc.)
  - Handle drafts vs published
- [ ] Custom CMS adapter template
  - HTTP endpoint for publishing
  - Metadata format spec
  - Retry logic for timeouts
- [ ] Supabase adapter (if client uses it)
  - Storage for markdown
  - Webhook trigger to frontend rebuild

**Impact**: Without this, publication is just database records. Nothing actually goes live.

**Estimate**: 3 days per adapter

---

### 9. Email Notifications 🟡 MEDIUM PRIORITY
**Current State**: No notifications  
**Needed**: Team awareness of key events

**Tasks**:
- [ ] Create `NotificationService`
  - Send email on review completion
  - Send email on publication success/failure
  - Send weekly performance report
  - Send alert on failed job
- [ ] Email templates (SendGrid or similar)
- [ ] Digest preferences (daily/weekly/never)
- [ ] Team notification channels (Slack webhook optional)

**Impact**: Without notifications, people don't know when content is ready or published.

**Estimate**: 2-3 days

---

### 10. API Documentation (OpenAPI/Swagger) 🟡 MEDIUM PRIORITY
**Current State**: README lists routes, no interactive docs  
**Needed**: Swagger/OpenAPI for all endpoints

**Tasks**:
- [ ] Add Swagger XML comments to all controllers
- [ ] Configure Swashbuckle in Startup
- [ ] Document all request/response DTOs
- [ ] Test Swagger UI (http://localhost:5000/swagger)
- [ ] Generate OpenAPI JSON for external tools

**Impact**: Without this, it's hard for frontend or external systems to use the API.

**Estimate**: 2 days

---

## Implementation Order (Critical Path)

**Week 1:**
1. Database migrations (production-ready schema)
2. Real LLM integration (Claude API)
3. Analytics integration (Google Analytics 4)

**Week 2:**
4. Authentication & user context
5. Backend error handling & logging
6. Frontend API connection

**Week 3:**
7. WordPress adapter
8. Unit tests for core services
9. Email notifications

**Optional (After core stable):**
10. Swagger documentation
11. Additional CMS adapters
12. Performance monitoring dashboards

---

## Why These Are Blockers

### For Phases 7-14 to work:

- **LLM Integration**: Phases 7 (competitive differentiation), 8 (persona angles), 9 (section regeneration), 11 (performance-guided generation) all need real LLM output
- **Analytics**: Phase 11 (continuous feedback) and Phase 6 validation depend on real performance data
- **Auth**: Competitive intelligence (Phase 7) and iterative refinement (Phase 9) need audit trails
- **Migrations**: Can't deploy to production without clean schema
- **Error Handling**: Publishing (Phase 5) and analytics sync (Phase 6) need robust error paths
- **Tests**: Iterative refinement (Phase 9) needs regression protection

### Without these:

- ❌ Can't measure if new features work (no real analytics data)
- ❌ Can't trust generated content (no quality validation)
- ❌ Can't troubleshoot failures (no logging)
- ❌ Can't deploy safely (no migrations, no tests)
- ❌ Can't track who did what (no auth/audit trail)

---

## Recommendation

**Do these 5 things first (2-3 week sprint):**

1. **Database migrations** (2 days) — Production-ready schema
2. **Real LLM integration** (3-4 days) — Replace mocks with Claude
3. **Analytics integration** (4-5 days) — Real performance data
4. **Auth system** (3 days) — User identity + audit trails
5. **Error handling & logging** (3-4 days) — Production observability

**Then move to Phases 7-14** — The foundation will be solid.

---

## Quick Wins (Do in Parallel)

- API documentation (2 days) — Helps frontend work in parallel
- Unit tests (4-5 days) — Build confidence while coding other features
- Email notifications (2-3 days) — Improves team experience

**Total estimated effort: 3-4 weeks to production-ready V3 core**

Then Phases 7-14 can be built on solid foundation without rework.
