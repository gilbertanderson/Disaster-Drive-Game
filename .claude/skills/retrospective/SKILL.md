---
name: retrospective
description: End-of-session retrospective that persists what was learned into CLAUDE.md - useful tools and MCP connections, project facts, gotchas and their fixes. Use when the user asks for a retrospective, to "update memory", or to capture learnings before ending a session.
---

# retrospective

Review the current session and fold anything durably useful into `CLAUDE.md`
(repo root), which Claude Code loads automatically at the start of every
session — that file IS the memory. Create it if it doesn't exist.

## 1. Review the session

Walk back through the conversation and collect candidates in four buckets:

- **Tools & connections** — MCP servers/tools that worked (exact tool names),
  CLIs that are (or notably are NOT) available in this environment, APIs hit
  and how they were authenticated (by secret *name* only, never values).
- **Project facts** — build/deploy paths, service IDs that are already public
  in the repo (e.g. `cloudProjectId`), branch conventions, where docs live,
  workflow trigger rules, known-failing tests.
- **Gotchas → fixes** — errors encountered and what actually resolved them.
  A gotcha without its fix is not worth saving.
- **Process** — how the user likes to work: PR conventions, who merges, what
  they consider out of scope.

Filter hard: keep only what would change how a future session acts. Skip
anything a future session would trivially rediscover, anything speculative,
and anything already covered by `AGENTS.md` or existing docs — link instead
of duplicating.

## 2. Update CLAUDE.md

- If `CLAUDE.md` doesn't exist, create it with this skeleton:

  ```markdown
  # CLAUDE.md

  Project guidance lives in [AGENTS.md](AGENTS.md); CI/deploy docs in
  [.github/CICD_SETUP.md](.github/CICD_SETUP.md). This file holds
  session-learned facts maintained by /retrospective.

  ## Session learnings
  ```

- Each entry is one bullet, one line if possible, dated: `- (YYYY-MM-DD) fact`.
- **Merge, don't append**: before adding, scan existing entries — update an
  entry that this session proved stale or wrong (correcting beats accumulating),
  and skip entries that already say the same thing.
- Keep the section under ~40 bullets; when over, delete the entries least
  likely to change future behavior (oldest first among the trivial ones).
- NEVER store secret values, tokens, passwords, or license contents. Secret
  *names* and where they're configured are fine.

## 3. Commit

Follow the repo's branching rules (AGENTS.md): never commit directly to
`main`/`dev`. If the session already has a working branch, commit there
(message: `Update CLAUDE.md session learnings`); otherwise create one and
open a PR.

## 4. Report

Tell the user what was added, updated, or pruned — a short list, not the
whole file.
