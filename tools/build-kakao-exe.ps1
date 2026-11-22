param([string]$Name = 'KakaoAutoPlay-v3', [string]$PythonPath = (Join-Path $env:LOCALAPPDATA 'Programs/Python/Python311/python.exe'))
$ErrorActionPreference = 'Stop'
if ($Name -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid output name' }
$project = Split-Path $PSScriptRoot -Parent
$python = $PythonPath
Push-Location $project
try {
    & $python (Join-Path $PSScriptRoot 'build-kakao-portable.py')
    if ($LASTEXITCODE) { throw 'Portable build failed' }
    @'
from pathlib import Path
import zipfile
root=Path('artifacts/KakaoAutoPlay-portable')
with zipfile.ZipFile('artifacts/kakao-exe-payload.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in root.rglob('*'):
        if not p.is_file() or '__pycache__' in p.parts or p.suffix=='.pyc': continue
        relative=p.relative_to(root).as_posix()
        if relative.startswith('artifacts/kakao-internals/') and not relative.startswith('artifacts/kakao-internals/deps/'): continue
        z.write(p,relative)
with zipfile.ZipFile('artifacts/kakao-exe-payload.zip') as z:
    assert z.testzip() is None
'@ | & $python -
    if ($LASTEXITCODE) { throw 'Payload build failed' }
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    $output = Join-Path $project ('artifacts/' + $Name + '.exe')
    $payload = Join-Path $project 'artifacts/kakao-exe-payload.zip'
    & $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$output" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll "/resource:$payload,payload.zip" (Join-Path $PSScriptRoot 'KakaoAutoPlayLauncher.cs') (Join-Path $PSScriptRoot 'KakaoAutoPlayForm.cs')
    if ($LASTEXITCODE) { throw 'Launcher compilation failed' }
    Get-FileHash -Algorithm SHA256 -LiteralPath $output
} finally { Pop-Location }
