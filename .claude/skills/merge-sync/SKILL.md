---
name: merge-sync
description: Synchronize the main and dev branches - merge main into dev, then dev back into main, so both end at the same commit. Use when the user asks to sync, reconcile, or cross-merge main and dev.
---

# merge-sync

Bring `main` and `dev` to the same state: first merge `main` into `dev`
(so `dev` picks up anything that landed on `main` directly), then merge
`dev` into `main` (so `main` picks up everything from `dev`). After a clean
run both branches point at the same tree.

This skill is the sanctioned exception to the "never commit directly to
`dev`/`main`" rule in AGENTS.md — it performs merges only, never edits files.

## Steps

1. **Preflight.** Require a clean working tree (`git status --porcelain` is
   empty); otherwise stop and tell the user what's dirty. Remember the current
   branch so you can return to it at the end.

2. **Fetch both branches.**

   ```bash
   git fetch origin main dev
   ```

3. **Merge main into dev.**

   ```bash
   git checkout -B dev origin/dev
   git merge --no-edit origin/main
   ```

   - If the merge reports conflicts: run `git merge --abort`, then report the
     conflicting files to the user and stop. Do NOT auto-resolve conflicts in
     Unity YAML assets (`.unity`, `.prefab`, `.asset`) — a wrong resolution
     silently corrupts scenes. Code/doc conflicts may be resolved by hand only
     if the user asks.
   - If dev already contains main, the merge is a no-op — continue.

4. **Push dev.**

   ```bash
   git push origin dev
   ```

   If the push is rejected because the remote moved, re-run from step 2.
   Never force-push.

5. **Merge dev into main.** This should now be a fast-forward, since dev
   contains main after step 3.

   ```bash
   git checkout -B main origin/main
   git merge --no-edit dev
   git push origin main
   ```

   Same conflict and rejected-push rules as above (conflicts here mean main
   moved mid-sync — re-run from step 2).

6. **Return and report.** Check out the branch you started on. Report the
   final commit both branches point at (`git rev-parse origin/main origin/dev`
   after a final fetch), whether each merge was a no-op, a fast-forward, or a
   merge commit, and anything that was skipped.

## Notes

- Pushes to `main`/`dev` will kick off the repo's workflows (tests, WebGL
  build, Build Automation trigger) — that's expected; mention it in the report.
- If branch protection blocks pushing directly to `main`, stop and tell the
  user; opening a PR from `dev` to `main` is the fallback, not a force-push.
