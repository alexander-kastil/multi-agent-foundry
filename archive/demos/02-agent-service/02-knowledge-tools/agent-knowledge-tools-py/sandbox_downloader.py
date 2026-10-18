import os
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()

class SandboxDownloader:
    def __init__(self, agents_client):
        self.agents_client = agents_client
        output_path = os.getenv("OUTPUT_PATH", "./output")
        downloads_folder = os.path.join(output_path, "downloads")
        self.downloads_dir = Path(downloads_folder).resolve()
        self.downloads_dir.mkdir(parents=True, exist_ok=True)

    def download(self, messages):
        for msg in messages:
            # Save every image file in the message
            for img in msg.image_contents:
                file_id = img.image_file.file_id
                file_name = f"{file_id}_image_file.png"
                file_path = self.downloads_dir / file_name
                try:
                    self.agents_client.files.save(file_id=file_id, file_name=str(file_path))
                    print(f"Saved image file to: {file_path}")
                except Exception as e:
                    print(f"Failed to save image {file_name}: {e}")

            # Save files from file-path annotations
            for ann in msg.file_path_annotations:
                if hasattr(ann.file_path, 'file_id') and ann.file_path.file_id:
                    file_id = ann.file_path.file_id
                    file_name = ann.text.split('/')[-1]  # Extract filename from path
                    file_path = self.downloads_dir / file_name
                    try:
                        self.agents_client.files.save(file_id=file_id, file_name=str(file_path))
                        print(f"Saved file from annotation to: {file_path}")
                    except Exception as e:
                        print(f"Failed to save file {file_name}: {e}")
                else:
                    print(f"No file_id for annotation: {ann.text}")

            # Print details of every file-path annotation
            for ann in msg.file_path_annotations:
                print("File Paths:")
                print(f"  Type: {ann.type}")
                print(f"  Text: {ann.text}")
                print(f"  File ID: {ann.file_path.file_id}")
                print(f"  Start Index: {ann.start_index}")
                print(f"  End Index: {ann.end_index}")