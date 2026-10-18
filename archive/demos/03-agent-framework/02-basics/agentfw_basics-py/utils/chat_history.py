"""
Chat history management utilities: reducers and message store with persistence.

This module provides reusable classes for managing chat history:
- SimpleMessage: a lightweight message representation
- ChatReducer: base class for reducers
- MessageCountingChatReducer: keeps only the most recent N messages
- SummarizingChatReducer: summarizes old messages using an LLM
- InMemoryChatMessageStore: message store with JSON serialization support
"""

import json
from dataclasses import dataclass, asdict
from typing import List, Optional
from pathlib import Path


@dataclass
class SimpleMessage:
    """Lightweight chat message representation."""
    role: str  # e.g., 'user' | 'assistant' | 'system'
    text: str


class ChatReducer:
    """Base reducer interface."""

    async def reduce(self, messages: List[SimpleMessage]) -> List[SimpleMessage]:
        """Return a reduced list of messages."""
        return messages


class MessageCountingChatReducer(ChatReducer):
    """Keep only the most recent `target_count` messages."""

    def __init__(self, target_count: int = 8):
        self.target_count = target_count

    async def reduce(self, messages: List[SimpleMessage]) -> List[SimpleMessage]:
        if len(messages) <= self.target_count:
            return messages
        # Keep the most recent target_count messages
        return messages[-self.target_count:]


class SummarizingChatReducer(ChatReducer):
    """Summarizes older messages into a single assistant summary message.

    This reducer calls a provided summarizer agent to generate a short summary
    of messages older than the `retain_last` messages when the total count is
    >= `threshold`.
    """

    def __init__(self, summarizer_agent, threshold: int = 8, retain_last: int = 4):
        self.summarizer_agent = summarizer_agent
        self.threshold = threshold
        self.retain_last = retain_last

    async def reduce(self, messages: List[SimpleMessage]) -> List[SimpleMessage]:
        if len(messages) < self.threshold:
            return messages

        # Split messages into old and recent
        keep_recent = messages[-self.retain_last :]
        to_summarize = messages[: -self.retain_last]

        # Build a prompt for summarization
        text_to_summarize = "\n\n".join(
            f"{m.role.upper()}: {m.text}" for m in to_summarize
        )

        summary_prompt = (
            "Summarize the following conversation history into a concise "
            "bullet-style summary that preserves key facts, decisions, and "
            "entities. Keep it short (one or two sentences) and suitable to be "
            "included as a single assistant message in the conversation history.\n\n"
            f"Conversation to summarize:\n{text_to_summarize}"
        )

        # Call summarizer agent
        try:
            summary_response = await self.summarizer_agent.run(summary_prompt)
            summary_text = summary_response.text if getattr(summary_response, "text", None) else str(summary_response)
            summary_text = (summary_text or "(no summary generated)").strip()
        except Exception as e:
            # If summarization fails, fall back to a synthetic note
            summary_text = f"[Summary failed: {e}]"

        # Create a single assistant message containing the summary
        summary_message = SimpleMessage(role="assistant", text=f"Summary: {summary_text}")

        # New history = summary + recent messages
        new_history = [summary_message] + keep_recent
        return new_history


class InMemoryChatMessageStore:
    """In-memory message store with JSON serialization support."""

    def __init__(self, reducers: Optional[List[ChatReducer]] = None, auto_save_path: Optional[str] = None):
        self._messages: List[SimpleMessage] = []
        self.reducers = reducers or []
        self.auto_save_path = auto_save_path

    async def add_message(self, message: SimpleMessage):
        """Add a message and apply reducers, then auto-save if configured."""
        self._messages.append(message)
        # Apply reducers (in order) after each addition
        for reducer in self.reducers:
            try:
                self._messages = await reducer.reduce(self._messages)
            except Exception:
                # Reducers are best-effort for demos
                pass
        
        # Auto-save after each message if path is configured
        if self.auto_save_path:
            try:
                self.save_to_file(self.auto_save_path)
            except Exception:
                # Auto-save is best-effort; don't crash on failure
                pass

    async def get_messages(self) -> List[SimpleMessage]:
        """Return all messages in the store."""
        return list(self._messages)

    def save_to_file(self, file_path: str):
        """Serialize messages to a JSON file."""
        Path(file_path).parent.mkdir(parents=True, exist_ok=True)
        messages_dict = [asdict(m) for m in self._messages]
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(messages_dict, f, indent=2, ensure_ascii=False)

    def load_from_file(self, file_path: str):
        """Deserialize messages from a JSON file."""
        if not Path(file_path).exists():
            raise FileNotFoundError(f"History file not found: {file_path}")
        
        with open(file_path, 'r', encoding='utf-8') as f:
            messages_dict = json.load(f)
        
        self._messages = [SimpleMessage(**m) for m in messages_dict]
