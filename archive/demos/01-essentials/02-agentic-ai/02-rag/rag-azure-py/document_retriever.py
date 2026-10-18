import json
from typing import Dict

from azure.core.exceptions import ResourceNotFoundError
from azure.storage.blob import BlobServiceClient


class DocumentRetriever:
    def __init__(
        self,
        blob_service_client: BlobServiceClient,
        container_name: str,
        blob_name: str,
    ):
        self.blob_service_client = blob_service_client
        self.container_name = container_name
        self.blob_name = blob_name
    
    def get_all_processed_documents(self) -> Dict:
        print(f"🔍 Attempting to retrieve from container: {self.container_name}")
        
        container_client = self.blob_service_client.get_container_client(self.container_name)
        if not container_client.exists():
            print(f"❌ Container '{self.container_name}' not found. Update PROCESSED_CONTAINER or STORAGE_CONTAINER_NAME.")
            return {}

        print(f"📥 Downloading file: {self.blob_name}")
        blob_client = container_client.get_blob_client(self.blob_name)

        try:
            if not blob_client.exists():
                print(f"❌ Blob '{self.blob_name}' not found in container '{self.container_name}'.")
                return {}

            blob_props = blob_client.get_blob_properties()
            file_size = blob_props.size
            print(f"✅ File found - Size: {file_size / (1024*1024):.2f} MB")

            print("📥 Downloading blob content...")
            blob_data = blob_client.download_blob().readall()

            print("🔄 Parsing JSON content...")
            documents = json.loads(blob_data.decode('utf-8'))

            print(f"✅ Successfully downloaded and parsed processed documents")
            print(f"📊 Found categories: {list(documents.keys())}")

            for category, docs in documents.items():
                successful_docs = [d for d in docs if d.get('success', False)]
                print(f"   - {category}: {len(successful_docs)}/{len(docs)} successful documents")

            return documents

        except ResourceNotFoundError:
            print(f"❌ Unable to locate '{self.blob_name}' in container '{self.container_name}'.")
        except json.JSONDecodeError as error:
            print(f"❌ Failed to parse JSON content: {error}")
        except Exception as error:  # noqa: BLE001
            print(f"❌ Unexpected error while downloading documents: {error}")

        return {}
