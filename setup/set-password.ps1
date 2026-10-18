# Script to set passwords for student accounts created by create-students.ps1
# Defaults to 10 users with UPN pattern: studentx01@<tenant-domain>

param(
    [int]$UserCount = 10,
    [string]$Password = "TiTp4student@#",
    [string]$Prefix = "student"
)

# Ensure Azure CLI is authenticated
try {
    $account = az account show 2>$null
    if (-not $account) {
        Write-Host "Not logged in to Azure. Please run 'az login' first." -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "Not logged in to Azure. Please run 'az login' first." -ForegroundColor Yellow
    exit 1
}

# Determine tenant domain from current account principal
$currentUser = az account show --query "user.name" -o tsv
if (-not $currentUser -or ($currentUser -notlike "*@*")) {
    Write-Host "Unable to determine domain from current account." -ForegroundColor Red
    exit 1
}
$domain = $currentUser.Split('@')[1]
Write-Host "Using domain: $domain"

Write-Host "Setting passwords for up to $UserCount users with prefix '$Prefix'..."

for ($i = 1; $i -le $UserCount; $i++) {
    $userPrincipal = "{0}{1:D2}@{2}" -f $Prefix, $i, $domain
    Write-Host "Processing $userPrincipal ..."

    # Check if user exists
    $null = az ad user show --id $userPrincipal 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  User not found. Skipping." -ForegroundColor DarkYellow
        continue
    }

    # Update password and ensure no forced rotation on next sign-in
    az ad user update --id $userPrincipal --password "$Password" --force-change-password-next-sign-in false 1>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Failed to set password." -ForegroundColor Red
    } else {
        Write-Host "  Password updated successfully." -ForegroundColor Green
    }
}

Write-Host "Done."