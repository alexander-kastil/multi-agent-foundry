import json
import mimetypes
import os
from pathlib import Path
from typing import Optional
from dotenv import load_dotenv
from azure.storage.blob import BlobServiceClient, ContentSettings

def get_blob_service_client() -> Optional[BlobServiceClient]:
    conn_str = os.environ.get("STORAGE_CONNECTION_STRING")
    if conn_str:
        return BlobServiceClient.from_connection_string(conn_str)

    account_name = os.environ.get("STORAGE_ACCOUNT_NAME")
    account_key = os.environ.get("STORAGE_ACCOUNT_KEY")
    if account_name and account_key:
        account_url = f"https://{account_name}.blob.core.windows.net"
        return BlobServiceClient(account_url=account_url, credential=account_key)

    return None


def upload_folder(container_name: str, assets_dir: Path) -> None:
    client = get_blob_service_client()
    container_client = client.get_container_client(container_name)

    files = sorted(p for p in assets_dir.iterdir() if p.is_file())
    if not files:
        return

    # Create processed documents JSON structure
    processed_documents = {
        "policies": []
    }

    for path in files:
        # Read file content
        with path.open("r", encoding="utf-8") as f:
            text_content = f.read()

        # Create document entry
        document = {
            "text": text_content,
            "success": True,
            "metadata": {
                "file_name": path.name,
                "file_type": "markdown"
            }
        }
        processed_documents["policies"].append(document)

    # Upload the processed documents JSON file
    blob_name = os.environ.get("PROCESSED_BLOB_NAME", "processed_documents_for_vectorization.json")
    json_data = json.dumps(processed_documents, indent=2)
    
    container_client.upload_blob(
        name=blob_name,
        data=json_data.encode("utf-8"),
        overwrite=True,
        content_settings=ContentSettings(content_type="application/json")
    )
    
    print(f"✅ Uploaded {len(processed_documents['policies'])} policy documents to {blob_name}")


def main() -> None:
    load_dotenv()
    assets_dir = Path(os.environ["ASSETS_DIR"])
    container_name = os.environ["STORAGE_CONTAINER_NAME"]
    upload_folder(container_name=container_name, assets_dir=assets_dir)


if __name__ == "__main__":
    main()
