import re
from typing import Any

from agent_framework import AgentSession, ContextProvider, SessionContext

PATTERN = re.compile(r"my favorite (\w+(?: \w+)?) is ([^.!?]+)", re.IGNORECASE)


class PreferenceProvider(ContextProvider):
    def __init__(self) -> None:
        super().__init__("preferences")

    async def before_run(
        self,
        *,
        agent: Any,
        session: AgentSession,
        context: SessionContext,
        state: dict[str, Any],
    ) -> None:
        preferences = state.get("preferences", {})
        if preferences:
            lines = "; ".join(f"{key}: {value}" for key, value in preferences.items())
            context.extend_instructions(self.source_id, f"Known user preferences: {lines}.")

    async def after_run(
        self,
        *,
        agent: Any,
        session: AgentSession,
        context: SessionContext,
        state: dict[str, Any],
    ) -> None:
        preferences = state.setdefault("preferences", {})
        for message in context.input_messages:
            for key, value in PATTERN.findall(message.text or ""):
                preferences[key.lower()] = value.strip()
