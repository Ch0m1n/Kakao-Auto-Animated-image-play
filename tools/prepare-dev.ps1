param([Parameter(Mandatory=$true)][string]$PythonPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& $PythonPath -c "import sys,struct; assert sys.version_info[:2]==(3,11) and struct.calcsize('P')==8, 'Python 3.11 x64 required'"
if ($LASTEXITCODE) { throw 'Unsupported Python runtime' }
$frida = Join-Path $root 'artifacts/kakao-internals/deps'
$pillow = Join-Path $root 'dist/codecs/deps'
New-Item -ItemType Directory -Force $frida,$pillow | Out-Null
& $PythonPath -m pip install --target $frida 'frida==17.27.4'
if ($LASTEXITCODE) { throw 'Frida dependency installation failed' }
& $PythonPath -m pip install --target $pillow 'pillow==13.0.0'
if ($LASTEXITCODE) { throw 'Pillow dependency installation failed' }
$fixture = Join-Path $root 'artifacts/tests/autoplay-agent-load'
New-Item -ItemType Directory -Force $fixture | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
& $compiler /nologo /target:winexe /platform:x86 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$fixture\KakaoTalk.exe" (Join-Path $root 'tests/Fixture.cs')
if ($LASTEXITCODE) { throw 'Native test fixture build failed' }
Write-Output 'Development dependencies and isolated x86 fixture are ready.'
