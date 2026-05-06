# AllO Cleanup Script
# Removes build artifacts and temporary files

$paths = @(
    "bin",
    "obj",
    ".vs",
    ".ruff_cache",
    "*.dll",
    "*.pdb",
    "*.exe",
    "*.suo",
    "*.user",
    "*.cache",
    "*.log",
    "Thumbs.db"
)

Write-Host "Cleaning AllO project..." -ForegroundColor Cyan

foreach ($path in $paths) {
    $items = Get-ChildItem -Path . -Include $path -Recurse -Force -ErrorAction SilentlyContinue
    if ($items) {
        $items | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
        Write-Host "  Cleaned: $path" -ForegroundColor Gray
    }
}

Write-Host "Cleanup complete!" -ForegroundColor Green
