FLOWS = [
    ("Agent calls Azure service (Cosmos DB, Foundry, Storage)", "managed identity"),
    ("Agent calls agent inside the same Azure trust boundary", "managed identity + workload identity"),
    ("Agent reads a user's Azure DevOps data", "on-behalf-of (OBO)"),
    ("Agent reads third-party SaaS data on a user's behalf", "OAuth2 authorization code + PKCE"),
    ("Agent calls a partner API that only accepts API keys", "key-based fallback with Key Vault"),
]

ROLES = {
    "style-checker": ["secrets/style-api-key"],
    "compliance-checker": ["secrets/compliance-api-key"],
}


class InMemoryVault:
    def __init__(self):
        self.secrets = {}

    def set_secret(self, name, value, enabled=True):
        self.secrets[name] = {"value": value, "enabled": enabled}

    def disable(self, name):
        self.secrets[name]["enabled"] = False

    def get_secret(self, name, identity):
        if f"secrets/{name.rsplit('-', 1)[0]}" not in ROLES[identity]:
            raise PermissionError(f"{identity} has no Key Vault Secrets User assignment for {name}")
        return self.secrets[name]


class KeyVaultSecretManager:
    def __init__(self, vault, identity):
        self.vault = vault
        self.identity = identity

    def get_current_api_key(self, secret_name):
        primary = self.vault.get_secret(f"{secret_name}-primary", self.identity)
        if primary["enabled"]:
            return primary["value"]
        return self.vault.get_secret(f"{secret_name}-secondary", self.identity)["value"]


print("Authentication flow selection")
for scenario, flow in FLOWS:
    print(f"  {scenario:<62} -> {flow}")

vault = InMemoryVault()
vault.set_secret("style-api-key-primary", "key-v1")
vault.set_secret("style-api-key-secondary", "key-v1")
vault.set_secret("compliance-api-key-primary", "compliance-key")

style = KeyVaultSecretManager(vault, "style-checker")
print("\nBlue-green rotation for style-checker")
print("  before rotation:", style.get_current_api_key("style-api-key"))

vault.set_secret("style-api-key-secondary", "key-v2")
vault.disable("style-api-key-primary")
print("  during rotation, primary disabled:", style.get_current_api_key("style-api-key"))

vault.set_secret("style-api-key-primary", "key-v2")
print("  after rotation, primary restored:", style.get_current_api_key("style-api-key"))

print("\nSecret-level RBAC")
try:
    style.vault.get_secret("compliance-api-key-primary", "style-checker")
except PermissionError as e:
    print(" ", e)
