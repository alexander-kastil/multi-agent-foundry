import json
import os
from datetime import datetime, timezone

STORE_PATH = os.path.join(os.path.dirname(__file__), "sessions.json")


def _load() -> dict:
    if not os.path.exists(STORE_PATH):
        return {}
    with open(STORE_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def save_session(session_id: str, conversation_id: str, last_response_id: str, metadata: dict) -> None:
    sessions = _load()
    sessions[session_id] = {
        "conversation_id": conversation_id,
        "last_response_id": last_response_id,
        "metadata": metadata,
        "updated_at": datetime.now(timezone.utc).isoformat(),
    }
    with open(STORE_PATH, "w", encoding="utf-8") as f:
        json.dump(sessions, f, indent=2)


def load_session(session_id: str) -> dict:
    return _load()[session_id]
