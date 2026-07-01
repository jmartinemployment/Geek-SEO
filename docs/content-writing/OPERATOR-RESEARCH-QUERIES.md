# Operator research queries (manual + automated)

Replace `{keyword}` with your article keyword (keep quotes for multi-word phrases).  
Replace `{domain}` with your site host (e.g. `geekatyourspot.com`).  
Replace `{local}` with your service area (default for GAYS: `San Francisco`).  
Replace `{after_date}` with a recent cutoff, e.g. `2024-01-01`.

**Junk filter** (append to most queries):

```text
-template -pdf -generator -reddit -quora -course -syllabus
```

---

## Citations (authoritative external sources)

```text
"{keyword}" site:en.wikipedia.org -template -pdf -generator -reddit -quora -course -syllabus

"{keyword}" (site:nist.gov OR site:ftc.gov OR site:usa.gov OR site:cdc.gov OR site:nih.gov) -template -pdf -generator -reddit -quora -course -syllabus

"{keyword}" site:edu -template -pdf -generator -reddit -quora -course -syllabus

"{keyword}" filetype:pdf site:edu -template -pdf -generator -reddit -quora -course -syllabus
```

## Own site (internal links — not external citations)

```text
site:{domain} "{keyword}"
```

## PAA supplement (extra reader questions)

Uses organic titles, PAA, and related searches from this SERP — not a substitute for live PAA on the main keyword SERP.

```text
"{keyword}" (how OR why OR what OR when OR cost OR vs) -template -pdf -generator -reddit -quora -course -syllabus
```

## Featured snippet hunt

```text
"what is {keyword}"

"{keyword}" definition
```

## News / timeliness

```text
"{keyword}" after:{after_date} -template -pdf -generator -reddit -quora
```

## Scholar (academic; SerpAPI `google_scholar` engine when automated)

```text
"{keyword}"
```

## Local angle (GAYS / SMB)

```text
"{keyword}" "small business" "{local}" -template -pdf -generator -reddit -quora -course
```

## Traditional vs AI contrast (section ideas)

```text
"{keyword}" spreadsheet OR workshop OR whiteboard -AI -template -pdf -generator
```

## Exclude junk (modifier — combine with other queries)

```text
-template -pdf -generator -reddit -quora -course -syllabus
```

---

GeekSeoBackend runs these automatically via `OperatorResearchEnricher` when loading research for writing and when fetching the document research pack.
