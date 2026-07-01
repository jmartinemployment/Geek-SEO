# Content critique — AI customer journey draft (Geek at Your Spot)

**Context:** Research-backed Content Writing draft for keyword space *“ai customer journey”* / use-case *“Create a map of my Customers Journey”*, with site profile for **geekatyourspot.com** (Delray Beach, tri-county SMB AI consultancy).

**Date:** 2026-06-30

This document preserves the product/content critique from the implementation review — before business voice pack, methodology, and PAA filtering fixes landed in code.

---

## What the generator produced (symptoms)

- Generic national marketing tone (e-commerce, hospitality vignettes)
- FAQs mirroring raw SERP PAA (*“Where can I find an AI customer journey PDF?”*, *“AI journey map generator?”*)
- Competitor-style H2 structure instead of four-phase methodology
- **Sources** block citing competitor article titles
- Fabricated authority (*“Dr. John Smith”*)
- No Delray Beach / Broward–Palm Beach–Miami-Dade grounding despite a rich site profile
- Chatbots and custom React/Node/Postgres capabilities mentioned in profile but not tied to *what Geek at Your Spot builds*

---

## What yesterday’s engineering fix did vs. what this critique asks for

| Layer | Generator fix (methodology + PAA filter) | This critique |
|--------|------------------------------------------|---------------|
| Structure | Four methodology H2s, not 15 competitor headings | Same — good foundation |
| SERP noise | Drop PDF/template/generator FAQs | Same — stops worst PAA junk |
| **Voice & proof** | Not addressed at time of critique | Concrete tools, old vs. AI contrast, build capabilities |
| **Conversion** | Not addressed at time of critique | Punchier intro, CTA tied to the topic |
| **Local ICP** | Geo in prompt as soft hint | Tri-county SMB who needs “what would *you* build for *me*?” |

**Takeaway:** The draft could be **well-structured SEO filler** and still fail as a **Geek at Your Spot landing page**. The generator optimized for “explain AI customer journey nationally,” not “sell Jeff’s shop in Delray.”

*(Subsequent work: **business voice pack** — gated concrete examples, traditional-vs-AI contrast, capability bridge, topic CTA — see [`HANDOFF.md`](HANDOFF.md).)*

---

## Point-by-point: critique is right for the ICP

### 1. Too abstract (“what data? what tool?”)

**South Florida SMB owners** (roughly 10–75 employees, ops/marketing lead, not a data team) don’t buy “insights.” They buy:

- A named stack they recognize (Shopify, HubSpot, QuickBooks, WordPress, GHL)
- A scenario (“tag review sentiment,” “route hot leads from chatbot to CRM”)
- Who implements it (you)

The site profile already had the raw material: chatbots, Postgres, React/Node, automation, tri-county service area. The draft **didn’t use it** because prompts listed “business context” in one paragraph without **requiring** stack-specific examples per section.

**Alignment:** Strong. This is how Geek at Your Spot should sound for Broward / Palm Beach / Miami-Dade professional services and light e-commerce.

**The fix (editorial):** Inject real-world tech examples. Instead of “e-commerce platforms use AI algorithms,” write something like: *“Using LLMs to tag customer sentiment on Shopify reviews”* or *“Predicting churn by feeding HubSpot data into custom Postgres analytics.”*

---

### 2. AI mapping vs. whiteboard mapping

Skeptical owners are the real audience. They *have* done sticky-note journeys. The article must answer: **“What changes when the data is live and the map updates?”**

That fits naturally in **Data Quality Assessment** and **Choose the Right AI**:

| Old way | AI way |
|---------|--------|
| Static personas from a workshop hunch | Cluster live support transcripts / CRM stages |
| One-off whiteboard map | Continuous refresh as new touchpoint data arrives |

**Alignment:** Very strong — differentiation, not more generic SEO.

**The fix (editorial):** Explicitly contrast the old way with the AI way. Example: *“Old way: static user personas based on a hunch. AI way: feeding 10,000 live customer support transcripts into a cluster to discover hidden pain points in real time.”*

---

### 3. Match full capabilities (custom apps + chatbots)

Mentioning chatbots under “hospitality” without tying to **“we design and deploy these on your stack”** reads like a textbook, not a consultancy.

For Geek at Your Spot, the pillar should read as: **“Here’s the map → here’s what we’d actually build (bot, dashboard, integration).”** That’s the Timex-to-AI story without needing the slogan in every paragraph — **veteran implementer**, not trend blogger.

**Alignment:** Strong for clients who need build + strategy; weak for “I only want a Canva template” — which is fine; that’s not the client.

**The fix (editorial):** Frame the article as a primer for the custom architectures you build. When discussing visualization of complex processes, add how a **custom dashboard** or **analytics pipeline** bridges the gap.

---

### 4. Structure & CTA

- **Shorter intro:** Two short opening paragraphs — direct answer first, no dense wall of text.
- **CTA tied to content:** Better than generic “Book a Free Strategy Call.”

**Example CTA:** *“Want to see what an AI journey map looks like for your specific tech stack? Let’s map your touchpoints on a free strategy call.”*

**Alignment:** High for conversion; lower impact on SEO indexing alone (product may eventually need **SEO pillar vs. landing variant** modes).

---

## South Florida alignment — direct answer

**It fits the clients you want** if the page does three things:

1. **Speaks tri-county SMB** — local service, compliance-sensitive shops, mixed tech maturity (WordPress + spreadsheets, not always enterprise data warehouses).
2. **Sells implementation** — not “learn journey mapping,” but “we map yours and wire the bots/pipelines.”
3. **Shows technical credibility without jargon walls** — named tools, simple before/after, one custom-build example.

It **does not** fit if the page stays national/generic (e-commerce + hospitality vignettes with no Florida, no HubSpot/Shopify, no “we build this”).

The niche description on the run was still **SERP-shaped** (“AI customer journey is the use of artificial intelligence…”), which pulls the writer toward **category SEO**, not **GAYS positioning**. Human edit + stronger site-level `WritingRecommendations` (from Site Analyzer) help.

---

## Human edit checklist (fast wins)

- [ ] One **old vs. AI** table or paired bullets per methodology section  
- [ ] Three **Florida-flavored** examples (e.g. service business: call → booking → follow-up)  
- [ ] One **capability bridge** (“We typically implement this as React dashboard + HubSpot webhooks + …”)  
- [ ] CTA: *“Want to see what an AI journey map looks like for your stack? Book a free strategy call.”*  
- [ ] Remove **Sources** and any **invented experts** (often from GEO score “Apply,” not the draft job)

---

## Final takeaway

The external critique described a **high-converting Geek at Your Spot page**; the generator output was closer to a **Frase-style national explainer**. For South Florida SMBs who need AI + automation **built**, alignment was **high in intent, low in execution** until business voice, methodology, and PAA gates shipped in the research-draft pipeline.

**Regenerate** after deploy to pick up generator fixes; still expect a **light human pass** for Timex-to-AI tone and client-specific proof.

---

## Related

- [`HANDOFF.md`](HANDOFF.md) — Content Writing operator workflow  
- [`docs/site-analyzer/HANDOFF.md`](../site-analyzer/HANDOFF.md) — Site Analyzer → Writer handoff  
