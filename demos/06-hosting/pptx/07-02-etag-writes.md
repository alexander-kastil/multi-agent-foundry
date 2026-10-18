---
slide: "07-02"
topic: 07-enterprise-a2a
title: "Shared state with ETags"
subtitle: "A write with a stale etag is rejected, re-read and retried"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/07-02-etag-writes.svg
order: 18
source: demos/06-hosting/07-enterprise-a2a/readme.md
gamma_prompt: >
  Slide titled "Shared state with ETags", subtitle "A write with a stale etag is rejected, re-read and
  retried". Two columns. Left column: the bullets. Right column, wider than the left: the provided sequence
  diagram in which two agents read one shared document at etag 1, the first write is accepted, the second
  write with the stale etag gets 412 Precondition Failed, and the second agent reads again and succeeds,
  shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never
  cropped, never used as a background or side accent. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Shared state with ETags

## Content

- Every read returns the document and its **`_etag`**, and every write sends that etag back
- The store accepts a write only while the document is still at that version
- A stale etag gets **412 Precondition Failed**, and the agent reads again and retries
- Each successful write publishes a **cache invalidation** message

## Speaker Notes

This slide shows two of the four agents in lab Step 2 writing one shared document at the same moment: the task `tech-analysis` of the tenant `acme-retirement`.

Read returns the etag: both agents read the document at `_etag` 1. The etag is a version tag that changes with every accepted write.

The guard: in the drawing, market-analysis-v2 writes first with etag 1, the store accepts it and the etag becomes 2. Which agent wins changes from run to run; the C# run in the guide needed three attempts for one agent.

412 and retry: risk-v1 still sends etag 1, so the store rejects it with 412 Precondition Failed, the Cosmos DB contract the in-memory store imitates. The retry reads again without the cache, because reusing the old document would write the very stale state the etag was meant to catch.

Invalidation: the store caches normal reads and publishes an invalidation message after each successful write. The lab run prints five of them, and four contributions survive in both languages.
