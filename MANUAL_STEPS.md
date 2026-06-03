# Manual Steps for Release

The GitHub MCP server cannot create tags or upload release assets.
Use one of the methods below.

## Method A: Push a tag (recommended)

The `.github/workflows/release.yml` workflow builds, tests, packages, and
creates a GitHub Release automatically when a `v*` tag is pushed.

```powershell
git tag v0.2.0
git push origin v0.2.0
```

The workflow will:
1. Build and test the solution.
2. Publish a self-contained win-x64 single-file `EjectLens.exe`.
3. Package it with README files and LICENSE into
   `EjectLens-v0.2.0-win-x64-portable.zip`.
4. Create a GitHub Release with the zip attached.

## Method B: Run workflow manually

1. Go to https://github.com/weinianxue/EjectLens/actions
2. Select the **Release** workflow.
3. Click **Run workflow** → **Run workflow** (branch: main).

## Method C: Manual upload

A pre-built zip is at:
  `artifacts/release/EjectLens-v0.2.0-win-x64-portable.zip`

1. Go to https://github.com/weinianxue/EjectLens/releases/new
2. Tag: `v0.2.0`, title: `EjectLens v0.2.0`
3. Copy release notes from `docs/release-notes/v0.2.0.md`
4. Upload the zip as the release asset.

## Important

- GitHub Code → Download ZIP gives source only (no exe). This is normal.
- Users should download from GitHub Releases.
- The release zip is NOT committed to the source repository.
