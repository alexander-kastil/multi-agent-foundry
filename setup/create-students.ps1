# Script to create student user accounts in Azure Entra, add them to a group, and assign Azure AI Developer role

# Number of student accounts to create (default 10)
$UserCount = 10

# Hardcoded account resource ID (parent of project - needed for visibility in portal)
$AccountResourceId = "/subscriptions/cd091145-5ea2-4703-ba5d-41063b1d4308/resourceGroups/rg-pro-code-agents/providers/Microsoft.CognitiveServices/accounts/pro-code-agents-resource"

# Hardcoded project resource ID
$ProjectResourceId = "/subscriptions/cd091145-5ea2-4703-ba5d-41063b1d4308/resourceGroups/rg-pro-code-agents/providers/Microsoft.CognitiveServices/accounts/pro-code-agents-resource/projects/pro-code-agents"

# Check if logged in to Azure
try {
    $account = az account show 2>$null
    if (-not $account) {
        Write-Host "Not logged in to Azure. Please run 'az login' first."
        exit 1
    }
} catch {
    Write-Host "Not logged in to Azure. Please run 'az login' first."
    exit 1
}

# Get the current user's domain
$currentUser = az account show --query "user.name" -o tsv
$domain = $currentUser.Split('@')[1]

Write-Host "Using domain: $domain"

# Create the group
Write-Host "Creating group 'pro-code-agents'..."
$groupId = az ad group create --display-name "pro-code-agents" --mail-nickname "pro-code-agents" --query "id" -o tsv
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create group."
    exit 1
}
Write-Host "Group created with ID: $groupId"

# Role IDs
$azureAIDeveloperRoleId = "64702f94-c441-49e6-a78b-ef80e0188fee"  # Azure AI Developer
$openAIUserRoleId = "5e0bd9bd-7b93-4f28-af87-19fc36ad61bd"         # Cognitive Services OpenAI User

# Prompt for password twice with confirmation
do {
    $password = Read-Host "Enter password for student accounts" -AsSecureString
    $passwordConfirm = Read-Host "Confirm password" -AsSecureString
    
    # Convert SecureString to plain text for comparison
    $passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))
    $passwordConfirmPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwordConfirm))
    
    if ($passwordPlain -ne $passwordConfirmPlain) {
        Write-Host "Passwords do not match. Please try again." -ForegroundColor Red
        $match = $false
    } else {
        $match = $true
    }
} while (-not $match)

$password = $passwordPlain

# Create users
for ($i = 1; $i -le $UserCount; $i++) {
    $userPrincipal = "studentx{0:D2}@{1}" -f $i, $domain
    $displayName = "Student $i"

    Write-Host "Creating user: $userPrincipal"
    $userId = az ad user create --display-name "$displayName" --user-principal-name "$userPrincipal" --password "$password" --force-change-password-next-sign-in false --query "id" -o tsv
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create user $userPrincipal"
        continue
    }

    # Add user to group
    Write-Host "Adding $userPrincipal to group..."
    az ad group member add --group "$groupId" --member-id "$userId"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to add $userPrincipal to group"
    } else {
        Write-Host "User $userPrincipal created and added to group."
    }

    # Assign Azure AI Developer role at account scope (for project visibility and model catalog)
    Write-Host "Assigning Azure AI Developer role at account scope to $userPrincipal..."
    az role assignment create --assignee $userPrincipal --role $azureAIDeveloperRoleId --scope $AccountResourceId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to assign account-scope role to $userPrincipal"
    } else {
        Write-Host "Account-scope role assigned to $userPrincipal."
    }

    # Assign Cognitive Services OpenAI User role at account scope (for model deployments)
    Write-Host "Assigning Cognitive Services OpenAI User role to $userPrincipal..."
    az role assignment create --assignee $userPrincipal --role $openAIUserRoleId --scope $AccountResourceId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to assign OpenAI User role to $userPrincipal"
    } else {
        Write-Host "OpenAI User role assigned to $userPrincipal."
    }

    # Assign Azure AI Developer role at project scope (for project-level operations)
    Write-Host "Assigning Azure AI Developer role at project scope to $userPrincipal..."
    az role assignment create --assignee $userPrincipal --role $azureAIDeveloperRoleId --scope $ProjectResourceId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to assign project-scope role to $userPrincipal"
    } else {
        Write-Host "Project-scope role assigned to $userPrincipal."
    }
}

Write-Host "Script completed."