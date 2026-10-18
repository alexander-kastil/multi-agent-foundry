$app = "copilot-retrieval-api"
$replyUri = "http://localhost:8080"
$supportedAcctTypes = "AzureADMyOrg"
$requiredAccessFile = Join-Path $PSScriptRoot "copilot-retrieval-api.json"
$requiredAccessArg = "@" + $requiredAccessFile

$clientId = az ad app create `
    --display-name $app `
    --sign-in-audience $supportedAcctTypes `
    --enable-id-token-issuance true `
    --enable-access-token-issuance true `
    --web-redirect-uris $replyUri `
    --required-resource-accesses $requiredAccessArg `
    --query appId `
    --output tsv

az ad app permission admin-consent --id $clientId
