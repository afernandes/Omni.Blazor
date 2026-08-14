[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$textsPath = Join-Path $repoRoot 'src/Omni.Blazor/Localization/OmniTexts.cs'
$resourceDirectory = Join-Path $repoRoot 'src/Omni.Blazor/Localization/Resources'
$keysPath = Join-Path $repoRoot 'src/Omni.Blazor/Localization/OmniTranslationKeys.cs'
$source = Get-Content -Raw -LiteralPath $textsPath

function ConvertFrom-CSharpString([string] $value) {
    [System.Text.RegularExpressions.Regex]::Unescape($value)
}

$portugueseMatches = [regex]::Matches(
    $source,
    'private string _valueFor(?<name>\w+) = "(?<value>(?:\\.|[^"])*)";')
$englishStart = $source.IndexOf('public static OmniTexts English()', [StringComparison]::Ordinal)
if ($englishStart -lt 0) { throw 'OmniTexts.English() was not found.' }
$englishMatches = [regex]::Matches(
    $source.Substring($englishStart),
    '(?m)^\s*(?<name>\w+) = "(?<value>(?:\\.|[^"])*)",')

$portuguese = [ordered]@{}
foreach ($match in $portugueseMatches) {
    $portuguese[$match.Groups['name'].Value] = ConvertFrom-CSharpString $match.Groups['value'].Value
}
$english = @{}
foreach ($match in $englishMatches) {
    $english[$match.Groups['name'].Value] = ConvertFrom-CSharpString $match.Groups['value'].Value
}

if ($portuguese.Count -ne $english.Count) {
    throw "Catalog count mismatch: pt-BR=$($portuguese.Count), en=$($english.Count)."
}
foreach ($key in $portuguese.Keys) {
    if (-not $english.ContainsKey($key)) { throw "English catalog is missing '$key'." }
}

$pluralVariants = [ordered]@{
    'DateRangeSummary' = @('{0} → {1} · {2} dia', '{0} → {1} · {2} dias', '{0} → {1} · {2} day', '{0} → {1} · {2} days')
    'DataGridFormSelectedCount' = @('{0} selecionado', '{0} selecionados', '{0} selected', '{0} selected')
    'DataImportReady' = @('{0:N0} linha pronta para importar.', '{0:N0} linhas prontas para importar.', '{0:N0} row ready to import.', '{0:N0} rows ready to import.')
    'DataImportUploadHint' = @('Até {0} e {1:N0} linha', 'Até {0} e {1:N0} linhas', 'Up to {0} and {1:N0} row', 'Up to {0} and {1:N0} rows')
    'DataImportPreviewLimit' = @('Exibindo a primeira {0:N0} de {1:N0} linha.', 'Exibindo as primeiras {0:N0} de {1:N0} linhas.', 'Showing the first {0:N0} of {1:N0} row.', 'Showing the first {0:N0} of {1:N0} rows.')
    'DataImportValidCount' = @('{0:N0} linha válida', '{0:N0} linhas válidas', '{0:N0} valid row', '{0:N0} valid rows')
    'DataImportInvalidCount' = @('{0:N0} linha inválida', '{0:N0} linhas inválidas', '{0:N0} invalid row', '{0:N0} invalid rows')
    'DataImportTotalCount' = @('{0:N0} linha no total', '{0:N0} linhas no total', '{0:N0} total row', '{0:N0} total rows')
    'DataFormMinimumItems' = @('Adicione pelo menos {0} item.', 'Adicione pelo menos {0} itens.', 'Add at least {0} item.', 'Add at least {0} items.')
    'DataFormMaximumItems' = @('Mantenha no máximo {0} item.', 'Mantenha no máximo {0} itens.', 'Keep at most {0} item.', 'Keep at most {0} items.')
    'MoreAppointments' = @('+ {0} mais', '+ {0} mais', '+ {0} more', '+ {0} more')
    'ItemsCount' = @('{0} de {1} item', '{0} de {1} itens', '{0} of {1} item', '{0} of {1} items')
    'ItemLimit' = @('Limite de {0} item', 'Limite de {0} itens', '{0} item limit', '{0} item limit')
    'HierarchyLimitReached' = @('Exibindo no máximo {0} linha.', 'Exibindo no máximo {0} linhas.', 'Showing at most {0} row.', 'Showing at most {0} rows.')
    'GroupLimitReached' = @('Exibindo no máximo {0} grupo.', 'Exibindo no máximo {0} grupos.', 'Showing at most {0} group.', 'Showing at most {0} groups.')
}
foreach ($entry in $pluralVariants.GetEnumerator()) {
    $portuguese["$($entry.Key).One"] = $entry.Value[0]
    $portuguese["$($entry.Key).Other"] = $entry.Value[1]
    $english["$($entry.Key).One"] = $entry.Value[2]
    $english["$($entry.Key).Other"] = $entry.Value[3]
}

function Get-PlaceholderIndexes([string] $value) {
    [System.Text.CompositeFormat]::Parse($value) | Out-Null
    $indexes = [regex]::Matches($value, '(?<!\{)\{(?<index>\d+)') |
        ForEach-Object { [int]$_.Groups['index'].Value } |
        Sort-Object -Unique
    return @($indexes)
}

foreach ($key in $portuguese.Keys) {
    $ptIndexes = @(Get-PlaceholderIndexes $portuguese[$key])
    $enIndexes = @(Get-PlaceholderIndexes $english[$key])
    if (($ptIndexes -join ',') -ne ($enIndexes -join ',')) {
        throw "Placeholder mismatch for '$key': pt-BR=[$($ptIndexes -join ',')], en=[$($enIndexes -join ',')]."
    }
}

function Write-Resx([string] $path, [System.Collections.IDictionary] $values) {
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($path, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('root')
        foreach ($header in @(
            @('resmimetype', 'text/microsoft-resx'),
            @('version', '2.0'),
            @('reader', 'System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'),
            @('writer', 'System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'))) {
            $writer.WriteStartElement('resheader')
            $writer.WriteAttributeString('name', $header[0])
            $writer.WriteElementString('value', $header[1])
            $writer.WriteEndElement()
        }
        foreach ($entry in $values.GetEnumerator()) {
            $writer.WriteStartElement('data')
            $writer.WriteAttributeString('name', [string]$entry.Key)
            $writer.WriteAttributeString('xml', 'space', $null, 'preserve')
            $writer.WriteElementString('value', [string]$entry.Value)
            $writer.WriteEndElement()
        }
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

Write-Resx (Join-Path $resourceDirectory 'OmniResources.resx') $portuguese
$orderedEnglish = [ordered]@{}
foreach ($key in $portuguese.Keys) {
    $orderedEnglish[$key] = $english[$key]
}
Write-Resx (Join-Path $resourceDirectory 'OmniResources.en.resx') $orderedEnglish

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('namespace Omni.Blazor.Localization;')
$lines.Add('')
$lines.Add('/// <summary>Stable keys used by Omni translation providers and catalogs.</summary>')
$lines.Add('public static class OmniTranslationKeys')
$lines.Add('{')
foreach ($match in $portugueseMatches) {
    $key = $match.Groups['name'].Value
    $lines.Add("    /// <summary>Translation key for <see cref=`"OmniTexts.$key`"/>.</summary>")
    $lines.Add("    public const string $key = nameof(OmniTexts.$key);")
}
$lines.Add('')
$lines.Add('    /// <summary>All stable base keys in declaration order.</summary>')
$lines.Add('    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly<string>')
$lines.Add('    ([')
foreach ($match in $portugueseMatches) { $lines.Add("        $($match.Groups['name'].Value),") }
$lines.Add('    ]);')
$lines.Add('')
$lines.Add('    /// <summary>All stable base and plural-variant keys in catalog order.</summary>')
$lines.Add('    public static IReadOnlyList<string> AllCatalogKeys { get; } = Array.AsReadOnly<string>')
$lines.Add('    ([')
foreach ($key in $portuguese.Keys) { $lines.Add("        `"$key`",") }
$lines.Add('    ]);')
$lines.Add('}')
Set-Content -LiteralPath $keysPath -Value $lines -Encoding utf8NoBOM

Write-Host "Generated $($portugueseMatches.Count) localization keys plus plural variants."
