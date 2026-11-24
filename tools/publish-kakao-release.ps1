param([Parameter(Mandatory=$true)][string]$Tag)
$ErrorActionPreference = 'Stop'
$version = (Get-Content version.json -Raw | ConvertFrom-Json).version
if ($Tag -ne "v$version" -or $version -notmatch '^\d+\.\d+\.\d+$') { throw 'Tag/version mismatch' }
$commit = git rev-parse HEAD
if ($LASTEXITCODE) { throw 'Cannot resolve release commit' }
$tagCommit = git rev-list -n 1 "refs/tags/$Tag" 2>$null
if ($LASTEXITCODE -eq 0 -and $tagCommit -ne $commit) { throw 'Release tag already points to different code' }
$releasesJson = gh release list --limit 100 --json tagName,isDraft,isPrerelease
if ($LASTEXITCODE) { throw 'Cannot check existing releases' }
$releases = $releasesJson | ConvertFrom-Json
$existing = $releases | Where-Object tagName -eq $Tag
if ($existing -and !$existing.isDraft) {
    if ($existing.isPrerelease) { throw 'Tag is already used by a prerelease' }
    Write-Output "$Tag is already published; keeping the existing release unchanged."
    exit 0
}
foreach ($release in $releases) {
    if (!$release.isDraft -and !$release.isPrerelease -and $release.tagName -match '^v?(\d+\.\d+\.\d+)$') {
        if ([version]$Matches[1] -ge [version]$version) { throw 'A newer or equal stable release already exists' }
    }
}
$expected = (Get-Content artifacts/SHA256SUMS.txt -Raw).Trim()
$actual = (Get-FileHash artifacts/KakaoAutoPlay.exe -Algorithm SHA256).Hash.ToLower()
if ($expected -ne "$actual  KakaoAutoPlay.exe") { throw 'Release artifact hash mismatch' }
if (!$existing) {
    gh release create $Tag --target $commit --draft --title "Kakao AutoPlay $Tag" --notes-file "docs/releases/$Tag.md"
    if ($LASTEXITCODE) { throw 'Release draft creation failed' }
}
gh release upload $Tag artifacts/KakaoAutoPlay.exe artifacts/SHA256SUMS.txt --clobber
if ($LASTEXITCODE) { throw 'Release upload failed; draft has not been published' }
gh release edit $Tag --draft=false --latest
if ($LASTEXITCODE) { throw 'Release publication failed' }
Write-Output "Published $Tag. Users can now select Update check."
