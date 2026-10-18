import os

import jwt
from jwt import PyJWKClient
from mcp.server.auth.provider import AccessToken

TENANT_ID = os.environ["TENANT_ID"]
API_CLIENT_ID = os.environ["API_CLIENT_ID"]
ISSUER = f"https://login.microsoftonline.com/{TENANT_ID}/v2.0"
JWKS = PyJWKClient(f"https://login.microsoftonline.com/{TENANT_ID}/discovery/v2.0/keys")


class EntraTokenVerifier:
    async def verify_token(self, token: str) -> AccessToken | None:
        try:
            key = JWKS.get_signing_key_from_jwt(token).key
            claims = jwt.decode(token, key, algorithms=["RS256"], audience=API_CLIENT_ID, issuer=ISSUER)
        except jwt.PyJWTError:
            return None
        return AccessToken(
            token=token,
            client_id=claims.get("azp", ""),
            scopes=claims.get("scp", "").split(),
            expires_at=claims["exp"],
        )
