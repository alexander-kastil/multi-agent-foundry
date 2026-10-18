import json
from types import SimpleNamespace
from typing import Optional, Tuple
from urllib import error, request

from azure.storage.blob import BlobServiceClient
from azure.search.documents import SearchClient
from azure.search.documents.indexes import SearchIndexClient
from azure.core.credentials import AzureKeyCredential
from azure.core.exceptions import ClientAuthenticationError, HttpResponseError, ResourceNotFoundError
from azure.ai.inference import EmbeddingsClient
from azure.identity import DefaultAzureCredential


def initialize_clients(
    storage_connection_string: str,
    search_endpoint: str,
    search_admin_key: str,
    search_index_name: str,
    inference_endpoint: str,
    inference_key: Optional[str],
    embeddings_model: str,
) -> Tuple[BlobServiceClient, SearchIndexClient, SearchClient, EmbeddingsClient]:
    blob_service_client = BlobServiceClient.from_connection_string(
        storage_connection_string
    )
    
    search_credential = AzureKeyCredential(search_admin_key)
    
    search_index_client = SearchIndexClient(
        endpoint=search_endpoint,
        credential=search_credential
    )
    
    search_client = SearchClient(
        endpoint=search_endpoint,
        index_name=search_index_name,
        credential=search_credential
    )
    
    if inference_key:
        embeddings_credential = AzureKeyCredential(inference_key)
        auth_note = "API key"
    else:
        embeddings_credential = DefaultAzureCredential()
        auth_note = "DefaultAzureCredential"

    embeddings_client = _create_embeddings_client(
        inference_endpoint=inference_endpoint,
        credential=embeddings_credential,
        embeddings_model=embeddings_model,
    )
    
    endpoint_label = "Azure OpenAI" if _is_openai_endpoint(inference_endpoint) else "Azure AI Inference"
    
    containers = list(blob_service_client.list_containers())
    print(f"✅ Connected to Blob Storage - Found {len(containers)} containers")
    
    print(f"✅ Connected to Azure AI Search - Service is available")
    print(f"✅ Connected to {endpoint_label} - Endpoint: {inference_endpoint} ({auth_note})")
    
    return blob_service_client, search_index_client, search_client, embeddings_client


def _create_embeddings_client(
    *,
    inference_endpoint: str,
    credential,
    embeddings_model: str,
):
    if not inference_endpoint:
        raise ValueError("An Azure AI endpoint must be configured.")

    if _is_openai_endpoint(inference_endpoint):
        return _AzureOpenAIEmbeddingsClient(
            endpoint=inference_endpoint,
            credential=credential,
            deployment_name=embeddings_model,
        )

    return EmbeddingsClient(
        endpoint=inference_endpoint,
        credential=credential,
        model=embeddings_model,
    )


def _is_openai_endpoint(endpoint: str) -> bool:
    return ".openai.azure.com" in (endpoint or "").lower()


class _AzureOpenAIEmbeddingsClient:
    """Minimal embeddings client for Azure OpenAI endpoints."""

    _API_VERSION = "2024-05-01-preview"
    _TOKEN_SCOPE = "https://cognitiveservices.azure.com/.default"

    def __init__(self, endpoint: str, credential, deployment_name: str):
        self._endpoint = (endpoint or "").rstrip("/")
        self._credential = credential
        self._deployment_name = deployment_name

    def embed(self, *, input, model=None):
        deployment = model or self._deployment_name
        if not deployment:
            raise ValueError("An embeddings deployment name must be provided.")

        payload = {"input": input}
        url = f"{self._endpoint}/openai/deployments/{deployment}/embeddings?api-version={self._API_VERSION}"
        headers = {"Content-Type": "application/json"}

        if isinstance(self._credential, AzureKeyCredential):
            headers["api-key"] = self._credential.key
        else:
            token = self._credential.get_token(self._TOKEN_SCOPE)
            headers["Authorization"] = f"Bearer {token.token}"

        request_data = json.dumps(payload).encode("utf-8")
        http_request = request.Request(url, data=request_data, headers=headers, method="POST")

        try:
            with request.urlopen(http_request, timeout=30) as response:
                result = json.loads(response.read().decode("utf-8"))
        except error.HTTPError as exc:
            error_body = exc.read().decode("utf-8")
            _raise_openai_error(status_code=exc.code, message=error_body)

        items = [SimpleNamespace(embedding=item.get("embedding", [])) for item in result.get("data", [])]
        return SimpleNamespace(data=items)


def _raise_openai_error(*, status_code: int, message: str) -> None:
    if status_code == 401:
        raise ClientAuthenticationError(message=message)
    if status_code == 404:
        raise ResourceNotFoundError(message=message)
    raise HttpResponseError(message=message, status_code=status_code)
