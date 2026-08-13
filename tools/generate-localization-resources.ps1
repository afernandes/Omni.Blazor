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

$portuguese['DateRangeSummary.One'] = '{0} → {1} · {2} dia'
$portuguese['DateRangeSummary.Other'] = '{0} → {1} · {2} dias'
$english['DateRangeSummary.One'] = '{0} → {1} · {2} day'
$english['DateRangeSummary.Other'] = '{0} → {1} · {2} days'

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
    if ($key -notlike 'DateRangeSummary.*') { $orderedEnglish[$key] = $english[$key] }
}
$orderedEnglish['DateRangeSummary.One'] = $english['DateRangeSummary.One']
$orderedEnglish['DateRangeSummary.Other'] = $english['DateRangeSummary.Other']
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
$lines.Add('    public static IReadOnlyList<string> All { get; } =')
$lines.Add('    [')
foreach ($match in $portugueseMatches) { $lines.Add("        $($match.Groups['name'].Value),") }
$lines.Add('    ];')
$lines.Add('}')
Set-Content -LiteralPath $keysPath -Value $lines -Encoding utf8NoBOM

Write-Host "Generated $($portugueseMatches.Count) localization keys plus plural variants."
