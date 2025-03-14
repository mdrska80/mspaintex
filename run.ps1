# Change to the MSPaintEx directory
Set-Location -Path "MSPaintEx"

Write-Host "Starting MSPaintEx with debug output..." -ForegroundColor Cyan
dotnet run 2>&1 | Out-Host

# Keep the window open if there was an error
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nPress any key to exit..." -ForegroundColor Red
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
} 