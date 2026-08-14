[CmdletBinding()]
param(
    [ValidateSet('json', 'resx', 'pot')]
    [string] $Format = 'json',

    [string] $Output,

    [ValidateSet('pt-BR', 'en')]
    [string] $SourceCulture = 'en',

    [string] $ResourceContext
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$resourceName = if ($SourceCulture -eq 'en') { 'OmniResources.en.resx' } else { 'OmniResources.resx' }
$resourcePath = Join-Path $repoRoot "src/Omni.Blazor/Localization/Resources/$resourceName"
$defaultOutput = Join-Path $repoRoot "artifacts/localization/omni-blazor.$Format"
if ([string]::IsNullOrWhiteSpace($Output)) { $Output = $defaultOutput }
$Output = [System.IO.Path]::GetFullPath($Output, $repoRoot)
$directory = Split-Path -Parent $Output
[System.IO.Directory]::CreateDirectory($directory) | Out-Null

[xml] $resource = Get-Content -Raw -LiteralPath $resourcePath
$entries = [ordered]@{}
foreach ($data in $resource.root.data) {
    $entries[[string]$data.name] = [string]$data.value
}

switch ($Format) {
    'json' {
        $entries | ConvertTo-Json -Depth 2 | Set-Content -LiteralPath $Output -Encoding utf8NoBOM
    }
    'resx' {
        $settings = [System.Xml.XmlWriterSettings]::new()
        $settings.Indent = $true
        $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
        $writer = [System.Xml.XmlWriter]::Create($Output, $settings)
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
            foreach ($entry in $entries.GetEnumerator()) {
                $writer.WriteStartElement('data')
                $writer.WriteAttributeString('name', [string]$entry.Key)
                $writer.WriteAttributeString('xml', 'space', $null, 'preserve')
                $writer.WriteElementString('value', '')
                $writer.WriteElementString('comment', [string]$entry.Value)
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()
            $writer.WriteEndDocument()
        }
        finally {
            $writer.Dispose()
        }
    }
    'pot' {
        function Escape-Po([string] $value) {
            $value.Replace('\', '\\').Replace('"', '\"').Replace("`r", '').Replace("`n", '\n')
        }

        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.Add('msgid ""')
        $lines.Add('msgstr ""')
        $lines.Add('"Content-Type: text/plain; charset=UTF-8\n"')
        $lines.Add('"Content-Transfer-Encoding: 8bit\n"')
        $lines.Add('')
        foreach ($entry in $entries.GetEnumerator()) {
            $lines.Add("#. $([string]$entry.Value -replace "`r?`n", ' ')")
            if (-not [string]::IsNullOrWhiteSpace($ResourceContext)) {
                $lines.Add("msgctxt `"$(Escape-Po $ResourceContext)`"")
            }
            $lines.Add("msgid `"$(Escape-Po ([string]$entry.Key))`"")
            $lines.Add('msgstr ""')
            $lines.Add('')
        }
        Set-Content -LiteralPath $Output -Value $lines -Encoding utf8NoBOM
    }
}

Write-Host "Exported $($entries.Count) localization keys to $Output"
