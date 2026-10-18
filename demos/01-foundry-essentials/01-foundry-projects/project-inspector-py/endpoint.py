import os
from urllib.parse import urlparse
from dotenv import load_dotenv

load_dotenv(override=True)

url = urlparse(os.environ["PROJECT_ENDPOINT"])
resource = url.hostname.split(".")[0]
project = url.path.rstrip("/").split("/")[-1]

print(f"Foundry resource : {resource}")
print(f"Project          : {project}")
print(f"Host             : {url.hostname}")
print(f"Path             : {url.path}")
print(f"Responses API    : {url.geturl().rstrip('/')}/openai/v1/responses")
