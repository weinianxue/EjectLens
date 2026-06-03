# Manual Steps

These steps are only needed when the automated release workflow cannot run
(e.g., GitHub Actions is unavailable or the MCP cannot create releases).

## Creating a release

### Option A: Push a version tag

The `.github/workflows/release.yml` workflow triggers automatically when a
tag matching `v*` is pushed (e.g., `v0.2.0`). It will:

1. Build and test the solution.
2. Publish a self-contained win-x64 single-file executable.
3. Package it with README files and LICENSE into a zip.
4. Create a GitHub Release with the zip attached.

To trigger it:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

### Option B: Manual dispatch

1. Go to https://github.com/weinianxue/EjectLens/actions
2. Select the **Release** workflow.
3. Click **Run workflow** → **Run workflow**.

### Option C: Fully manual release

1. Build and publish locally:

   ```powershell
   dotnet publish src/EjectLens/EjectLens.csproj -c Release -r win-x64 `
     --self-contained true -p:PublishSingleFile=true `
     -p:EnableCompressionInSingleFile=true -o artifacts/publish/win-x64/EjectLens
   ```

2. Copy README.md, README.zh-CN.md, and LICENSE into the publish folder.

3. Create a zip containing the `EjectLens/` folder.

4. Go to https://github.com/weinianxue/EjectLens/releases/new
   and upload the zip as a release asset.

## Important notes

- The GitHub source code ZIP (Code → Download ZIP) contains only source
  files, not an executable. This is normal and expected.
- Users should always download the release package from GitHub Releases.
- The release zip contains `EjectLens.exe` (self-contained single file),
  README files, and the license.
