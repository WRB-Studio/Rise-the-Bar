Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

$previousEditorPath = $env:UNITY_EDITOR_PATH
try {
    $env:UNITY_EDITOR_PATH = $null
    $expected = 'X:\Unity\6000.3.25f1\Editor\Unity.exe'
    function Test-Path {
        param($LiteralPath, $PathType)
        return $LiteralPath -eq $expected -or $LiteralPath -like '*editors-v2.json'
    }
    function Get-Content {
        param($LiteralPath, [switch]$Raw)
        if ($LiteralPath -like '*ProjectVersion.txt') { return 'm_EditorVersion: 6000.3.25f1' }
        if ($LiteralPath -like '*editors-v2.json') {
            return '{"data":[{"version":"6000.3.15f1","location":["X:/wrong-version/Unity.exe"]},{"version":"6000.3.25f1","location":["X:/missing/Unity.exe","X:\\Unity\\6000.3.25f1\\Editor\\Unity.exe"]}]}'
        }
        throw 'Unexpected configuration read.'
    }
    function Resolve-Path { param($LiteralPath) return [pscustomobject]@{ Path = $LiteralPath } }
    if ((Resolve-UnityEditor) -ne $expected) { throw 'Unity Hub catalog fallback chose the wrong Editor.' }
    $rejected = $false
    try { Resolve-UnityEditor -UnityPath 'X:/missing/Unity.exe' | Out-Null }
    catch { $rejected = $_.Exception.Message -like 'Unity executable not found:*' }
    if (-not $rejected) { throw 'Invalid explicit Editor override was silently replaced.' }
    Write-Output 'Passed: Unity Hub catalog fallback selects the project version and existing executable; invalid explicit overrides remain errors. No Editor launch.'
} finally {
    $env:UNITY_EDITOR_PATH = $previousEditorPath
}
