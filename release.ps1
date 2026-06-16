#requires -Version 7
<#
.SYNOPSIS
    Publica uma nova versão do Omni.Blazor criando e empurrando uma tag git.

.DESCRIPTION
    O workflow `.github/workflows/release.yml` dispara ao receber uma tag `vX.Y.Z`
    e publica no NuGet.org (via Trusted Publishing / OIDC) + GitHub Packages, e cria
    o GitHub Release. A versão do pacote é derivada da tag pelo MinVer — você NÃO edita
    <Version> em csproj.

    Este script faz a parte local com segurança:
      1. confere que você está na branch certa, com working tree limpo e em dia com o origin;
      2. resolve a versão (explícita via -Version, ou incrementando a última tag via -Bump);
      3. valida o formato SemVer e que a tag ainda não existe;
      4. roda os testes (a menos que -SkipTests);
      5. cria a tag anotada e faz o push (dispara a release);
      6. acompanha o workflow no GitHub (se o gh CLI estiver instalado).

    Commit suas mudanças ANTES de rodar — a tag empacota o commit atual.

.PARAMETER Version
    Versão explícita: X.Y.Z ou X.Y.Z-prerelease (ex.: 0.2.0, 0.3.0-beta.1).
    Aceita com ou sem o prefixo 'v'.

.PARAMETER Bump
    Em vez de -Version, incrementa a partir da última tag estável: patch | minor | major.

.PARAMETER SkipTests
    Pula `dotnet test` antes de taggar.

.PARAMETER DryRun
    Mostra o que faria, sem criar nem empurrar a tag.

.PARAMETER Yes
    Não pede confirmação interativa (use com cuidado — a publicação é permanente).

.PARAMETER NoWatch
    Não acompanha o workflow após o push.

.PARAMETER Branch
    Branch de release (padrão: main).

.EXAMPLE
    ./release.ps1 -Bump patch          # 0.1.0 -> 0.1.1

.EXAMPLE
    ./release.ps1 -Version 0.2.0       # versão explícita

.EXAMPLE
    ./release.ps1 -Version 0.3.0-beta.1 -SkipTests

.EXAMPLE
    ./release.ps1 -Bump minor -DryRun  # só mostra o que faria
#>
[CmdletBinding(DefaultParameterSetName = 'Explicit')]
param(
    [Parameter(ParameterSetName = 'Explicit', Position = 0)]
    [string]$Version,

    [Parameter(ParameterSetName = 'Bump', Mandatory)]
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Bump,

    [switch]$SkipTests,
    [switch]$DryRun,
    [switch]$Yes,
    [switch]$NoWatch,
    [string]$Branch = 'main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Controlamos erros de comandos nativos manualmente via $LASTEXITCODE (não auto-throw).
$PSNativeCommandUseErrorActionPreference = $false

# ---- helpers -------------------------------------------------------------
function Info($m) { Write-Host "→ $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "✓ $m" -ForegroundColor Green }
function Warn($m) { Write-Host "! $m" -ForegroundColor Yellow }
function Die($m)  { Write-Host "✗ $m" -ForegroundColor Red; exit 1 }

function Exec {
    param([Parameter(Mandatory)][string]$File, [string[]]$Arguments)
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { Die "Comando falhou ($LASTEXITCODE): $File $($Arguments -join ' ')" }
}

# ---- pré-requisitos ------------------------------------------------------
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Die "git não encontrado no PATH." }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Die "dotnet não encontrado no PATH." }
$hasGh = [bool](Get-Command gh -ErrorAction SilentlyContinue)

# Roda a partir da pasta do script (que deve ser a raiz do repo).
Set-Location $PSScriptRoot
git rev-parse --is-inside-work-tree *> $null
if ($LASTEXITCODE -ne 0) { Die "Não é um repositório git: $PSScriptRoot" }

# URL web do repo (para links), derivada do origin.
$originUrl = (git remote get-url origin).Trim()
$webUrl = ($originUrl -replace '\.git$', '') -replace '^git@github\.com:', 'https://github.com/'

# ---- estado do repositório ----------------------------------------------
$current = (git rev-parse --abbrev-ref HEAD).Trim()
if ($current -ne $Branch) { Die "Você está em '$current'. Faça checkout em '$Branch' (releases saem da '$Branch')." }

if (git status --porcelain) {
    Die "Working tree sujo — commit ou stash antes de gerar a release (a tag empacota o commit atual)."
}

Info "Buscando refs do origin…"
Exec git @('fetch', '--tags', '--prune', '--quiet', 'origin')
$local  = (git rev-parse $Branch).Trim()
$remote = (git rev-parse "origin/$Branch").Trim()
if ($local -ne $remote) {
    Die "'$Branch' local ($($local.Substring(0,7))) difere de 'origin/$Branch' ($($remote.Substring(0,7))). Faça pull/push antes."
}
Ok "Branch '$Branch' limpa e em dia com origin."

# ---- resolve a versão ----------------------------------------------------
function Get-LatestStableTag {
    git tag --list 'v*' --sort='-v:refname' |
        Where-Object { $_ -match '^v\d+\.\d+\.\d+$' } |
        Select-Object -First 1
}

if ($PSCmdlet.ParameterSetName -eq 'Bump') {
    $latest = Get-LatestStableTag
    if (-not $latest) { Die "Nenhuma tag estável (vX.Y.Z) encontrada — use -Version para a primeira." }
    if ($latest -notmatch '^v(\d+)\.(\d+)\.(\d+)$') { Die "Não consegui parsear a última tag '$latest'." }
    $maj = [int]$Matches[1]; $min = [int]$Matches[2]; $pat = [int]$Matches[3]
    switch ($Bump) {
        'major' { $maj++; $min = 0; $pat = 0 }
        'minor' { $min++; $pat = 0 }
        'patch' { $pat++ }
    }
    $Version = "$maj.$min.$pat"
    Info "Última tag: $latest  →  nova versão ($Bump): $Version"
}
elseif (-not $Version) {
    Die "Informe -Version <x.y.z> ou -Bump <patch|minor|major>. (Veja: Get-Help ./release.ps1)"
}

# normaliza (remove 'v' inicial) + valida SemVer (pré-release opcional)
$Version = $Version.TrimStart('v', 'V')
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    Die "Versão inválida: '$Version'. Use X.Y.Z ou X.Y.Z-prerelease (ex.: 1.2.3, 1.2.3-beta.1)."
}
$tag = "v$Version"
$isPre = $Version.Contains('-')

# tag já existe (local ou remoto)?
git rev-parse -q --verify "refs/tags/$tag" *> $null
if ($LASTEXITCODE -eq 0) { Die "A tag $tag já existe localmente." }
if (git ls-remote --tags origin "refs/tags/$tag") { Die "A tag $tag já existe no origin." }

# ---- testes --------------------------------------------------------------
if (-not $SkipTests) {
    Info "Rodando testes (dotnet test, Release)… (use -SkipTests para pular)"
    Exec dotnet @('test', 'test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj', '-c', 'Release', '--nologo')
    Ok "Testes verdes."
}
else {
    Warn "Testes pulados (-SkipTests)."
}

# ---- confirmação ---------------------------------------------------------
Write-Host ""
Info "Pronto para publicar:"
Write-Host "    Pacote : Omni.Blazor"
Write-Host "    Versão : $Version$(if ($isPre) { '   (pré-release)' })"
Write-Host "    Tag    : $tag  →  commit $($local.Substring(0,7))"
Write-Host "    Efeito : o push da tag dispara o workflow Release → publica no NuGet.org + GitHub Packages."
Write-Host ""

if ($DryRun) { Warn "DryRun: nada foi criado nem empurrado."; exit 0 }

if (-not $Yes) {
    $ans = Read-Host "Confirmar? A publicação no NuGet.org é permanente [y/N]"
    if ($ans -notmatch '^(y|s|sim|yes)$') { Die "Cancelado." }
}

# ---- tag + push ----------------------------------------------------------
Info "Criando tag anotada $tag…"
Exec git @('tag', '-a', $tag, '-m', "Omni.Blazor $Version")
Info "Publicando a tag (dispara a release)…"
Exec git @('push', 'origin', $tag)
Ok "Tag $tag publicada."

# ---- acompanhar ----------------------------------------------------------
if ($NoWatch -or -not $hasGh) {
    if (-not $hasGh) { Warn "gh CLI não encontrado — acompanhe manualmente." }
    Write-Host "Actions: $webUrl/actions"
    exit 0
}

Info "Localizando o run da release…"
$runId = $null
for ($i = 0; $i -lt 12 -and -not $runId; $i++) {
    Start-Sleep -Seconds 5
    $runId = gh run list --workflow=release.yml --limit 10 --json databaseId, headBranch |
        ConvertFrom-Json |
        Where-Object { $_.headBranch -eq $tag } |
        Select-Object -First 1 -ExpandProperty databaseId
}
if (-not $runId) {
    Warn "Run ainda não apareceu. Acompanhe em: $webUrl/actions"
    exit 0
}

Info "Acompanhando run $runId (Ctrl+C sai daqui; a release continua no servidor)…"
gh run watch $runId --exit-status
if ($LASTEXITCODE -eq 0) {
    Ok "Release $Version concluída! 🚀  https://www.nuget.org/packages/Omni.Blazor/$Version"
}
else {
    Die "A release falhou. Detalhes: gh run view $runId --log-failed"
}
