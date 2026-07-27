# Content Writer V3 — Phase 4: Review & Approval

## Problem Phase 4 Solves

**V2 Issue:** Drafts published without human verification. No editorial gate to catch:
- Factual errors or unsupported claims
- Brand voice misalignment
- Missing or weak CTAs
- Competitive positioning gaps
- Offering mismatches

**V3 Fix:** Structured editorial review with scoring, comment threading, and decision gates.

---

## Phase 4 Architecture

### Entities

**ContentReview**
- AssetVersionId → which draft
- ReviewedByUserId → who reviewed
- Status: Pending → ChangesRequested → Approved → Rejected
- Scores (1-10):
  - AccuracyScore: How fact-checked?
  - StrengthScore: How compelling/differentiated?
  - AlignmentScore: How well positioned?
- EditorSummary: Overall editorial assessment
- ApprovedAt: When approved
- ReviewedAt: When review completed

**ReviewComment**
- ReviewId → parent review
- AuthorId → who wrote the comment
- Section: Which insight/heading (e.g., "Emergency Response Window")
- LineNumber: Optional line reference
- CommentText: Detailed feedback
- Type: Factual | Tone | Clarity | Structure | CTA | Evidence | Offering | Competitive
- Severity: Blocker | Major | Minor | Suggestion
- Resolved: Is this comment addressed?
- Resolution: How was it addressed?

### Workflow

```
Draft Created
    ↓
[Review Initiated]
    ↓
[Editor Reviews] → [Scores Accuracy/Strength/Alignment] → [Threads Comments]
    ↓
Decision:
  - Approve → Phase 5 (Publish)
  - Request Changes → Back to Writer (add feedback loop)
  - Reject → Marked failed, restart
```

### Quality Gates

- **Accuracy < 6/10?** → Blocker: Review must detail specific unsupported claims
- **Strength < 5/10?** → Major: Reviewer explains why it's generic/weak
- **Alignment < 6/10?** → Blocker: Positioning issues before publication
- **Any Blocker Comments?** → Can't approve (must be resolved)

### Review Request Flow

1. Draft completed by ContentWriterV3JobWorker
2. ContentReview entity created (Status=Pending)
3. Editor opens review UI
4. Editor sees:
   - Draft content
   - Validation warnings/recommendations from Phase 3
   - Previous strategy brief (angle, audience, positioning)
   - Related existing content (for consistency check)
5. Editor adds comments (inline or section-level)
6. Editor scores accuracy/strength/alignment
7. Editor approves, requests changes, or rejects
8. If approved → proceed to Phase 5 (Publish)
9. If rejected/changes → notify writer, allow resubmit

### API Endpoints

**POST /api/content-writer/v3/reviews/{assetVersionId}/initiate**
- Create review for a draft
- Returns ReviewId

**GET /api/content-writer/v3/reviews/{reviewId}**
- Full review with comments

**POST /api/content-writer/v3/reviews/{reviewId}/comment**
- Add editorial comment
- Params: section, text, type, severity

**PATCH /api/content-writer/v3/reviews/{reviewId}/score**
- Update accuracy/strength/alignment scores
- Params: accuracyScore, strengthScore, alignmentScore

**POST /api/content-writer/v3/reviews/{reviewId}/approve**
- Approve and move to Phase 5

**POST /api/content-writer/v3/reviews/{reviewId}/request-changes**
- Request changes (returns to writer)

**POST /api/content-writer/v3/reviews/{reviewId}/reject**
- Reject review
- Params: reason

### Exit Criteria

- ✓ Review scores recorded
- ✓ All Blocker comments resolved
- ✓ Approval recorded with timestamp
- ✓ Ready for Phase 5 (Publication)
