$json = Get-Content 'D:\source\FluentJson\tests\FluentJson.SystemTextJson.Tests\StrykerOutput\2026-03-15.18-59-58\reports\mutation-report.json' -Raw | ConvertFrom-Json
foreach ($prop in $json.files.psobject.properties) {
    if ($prop.Value.mutants -ne $null) {
        foreach ($mutant in $prop.Value.mutants) {
            if ($mutant.status -eq 'Survived') {
                Write-Host "Mutant in $($prop.Name) line $($mutant.location.start.line): $($mutant.mutatorName) -> $($mutant.replacement)"
            }
        }
    }
}
