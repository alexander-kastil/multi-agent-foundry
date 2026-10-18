import base64
import json
from azure.identity import DefaultAzureCredential

token = DefaultAzureCredential().get_token("https://ai.azure.com/.default").token
payload = token.split(".")[1]
claims = json.loads(base64.urlsafe_b64decode(payload + "=" * (-len(payload) % 4)))

print(f"audience : {claims['aud']}")
print(f"identity : {claims.get('upn') or claims.get('appid')}")
print(f"type     : {claims['idtyp']}")
print(f"tenant   : {claims['tid'][:8]}...")
