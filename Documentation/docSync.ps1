while ($true) {
    pandoc ".\BuildingSketcherDocumentation.docx" -o ".\BuildingSketcherDocumentation.md" --extract-media=media
    Write-Output "Documentation updated at $(Get-Date)"
    Start-Sleep -Seconds 300
}