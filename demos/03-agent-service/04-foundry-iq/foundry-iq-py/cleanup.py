import requests
from azure.identity import get_bearer_token_provider
from azure.search.documents.indexes import SearchIndexClient

from common import API_VERSION, CONNECTION, KNOWLEDGE_BASE, PROJECT_RESOURCE_ID, SEARCH_ENDPOINT, SOURCES, credential

index_client = SearchIndexClient(endpoint=SEARCH_ENDPOINT, credential=credential, api_version=API_VERSION)

print("== Cleanup")
index_client.delete_knowledge_base(KNOWLEDGE_BASE)
print(f"deleted knowledge base {KNOWLEDGE_BASE}")
for index_name, source_name in SOURCES.items():
    index_client.delete_knowledge_source(source_name)
    index_client.delete_index(index_name)
    print(f"deleted knowledge source {source_name} and index {index_name}")

management_token = get_bearer_token_provider(credential, "https://management.azure.com/.default")()
requests.delete(
    f"https://management.azure.com{PROJECT_RESOURCE_ID}/connections/{CONNECTION}?api-version=2025-10-01-preview",
    headers={"Authorization": f"Bearer {management_token}"},
).raise_for_status()
print(f"deleted connection {CONNECTION}")
