import os
import subprocess
import sys
import time

import httpx
from dotenv import load_dotenv

load_dotenv()
PORT = os.getenv("MCP_PORT", "8765")
API_KEY = os.getenv("MCP_API_KEY")
BASE = f"http://127.0.0.1:{PORT}"
HEADERS = {"x-api-key": API_KEY}


def section(title):
    print(f"\n=== {title} ===")


def admin(path):
    return httpx.post(f"{BASE}{path}", headers=HEADERS).json()


def show(label):
    health = httpx.get(f"{BASE}/health", headers=HEADERS).json()
    metrics = httpx.get(f"{BASE}/metrics", headers=HEADERS).json()
    print(f"{label}: circuit={health['circuit']} routing={metrics['routing']} p95_ms={metrics['p95_latency_ms']}")


def start_server():
    server = subprocess.Popen([sys.executable, "server.py"])
    while True:
        try:
            if httpx.get(f"{BASE}/health", headers=HEADERS).status_code == 200:
                return server
        except httpx.TransportError:
            time.sleep(0.3)
