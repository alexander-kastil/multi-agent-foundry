import time
from contextvars import ContextVar
import jwt
from cryptography.hazmat.primitives.asymmetric import rsa

EXPECTED_AUDIENCE = "api://fabrikam-code-review"
EXPECTED_ISSUER = "https://login.microsoftonline.com/fabrikam-tenant/v2.0"

current_tenant = ContextVar("current_tenant", default=None)


def new_signing_key():
    return rsa.generate_private_key(public_exponent=65537, key_size=2048)


def issue_token(private_key, tenant_id):
    return jwt.encode(
        {
            "aud": EXPECTED_AUDIENCE,
            "iss": EXPECTED_ISSUER,
            "exp": int(time.time()) + 300,
            "tenant_id": tenant_id,
        },
        private_key,
        algorithm="RS256",
    )


def extract_tenant_from_token(authorization_header, public_key):
    token = authorization_header.split("Bearer ")[1]
    decoded = jwt.decode(
        token,
        public_key,
        algorithms=["RS256"],
        audience=EXPECTED_AUDIENCE,
        issuer=EXPECTED_ISSUER,
    )
    return decoded["tenant_id"]


class PartitionedStore:
    def __init__(self):
        self.partitions = {}

    def create_item(self, document, partition_key):
        self.partitions.setdefault(partition_key, {})[document["id"]] = document

    def read_item(self, item, partition_key):
        return self.partitions.get(partition_key, {})[item]

    def query_items(self, partition_key):
        return list(self.partitions.get(partition_key, {}).values())


class CodeReviewRepository:
    def __init__(self, store):
        self.store = store

    def save_review(self, review_id, code_file, findings):
        tenant_id = current_tenant.get()
        self.store.create_item(
            {"id": review_id, "tenant_id": tenant_id, "code_file": code_file, "findings": findings},
            partition_key=tenant_id,
        )

    def get_review(self, review_id):
        tenant_id = current_tenant.get()
        if review_id not in self.store.partitions.get(tenant_id, {}):
            return None
        return self.store.read_item(review_id, partition_key=tenant_id)

    def list_reviews(self):
        return self.store.query_items(partition_key=current_tenant.get())
