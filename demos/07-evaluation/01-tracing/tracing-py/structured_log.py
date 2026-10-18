import hashlib
import json
import logging
from datetime import datetime, timezone
from opentelemetry import trace
from opentelemetry.trace import Status, StatusCode

logging.basicConfig(level=logging.INFO, format="%(message)s")
logging.getLogger("azure").setLevel(logging.WARNING)
logging.getLogger("httpx").setLevel(logging.WARNING)
logging.getLogger("httpx2").setLevel(logging.WARNING)
logger = logging.getLogger("agent-decisions")


class StructuredLogger:
    def __init__(self, agent_id, agent_version):
        self.agent_id = agent_id
        self.agent_version = agent_version

    def log(self, operation_type, status, session_id=None, customer_tier=None,
            input_token_count=0, output_token_count=0, latency_ms=0, error_code=None, **fields):
        span = trace.get_current_span()
        context = span.get_span_context()
        entry = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "trace_id": format(context.trace_id, "032x"),
            "span_id": format(context.span_id, "016x"),
            "agent_id": self.agent_id,
            "agent_version": self.agent_version,
            "operation_type": operation_type,
            "status": status,
            "session_id_hash": hashlib.sha256(session_id.encode()).hexdigest()[:16] if session_id else None,
            "customer_tier": customer_tier,
            "input_token_count": input_token_count,
            "output_token_count": output_token_count,
            "latency_ms": latency_ms,
            "error_code": error_code,
            **fields,
        }
        entry = {key: value for key, value in entry.items() if value is not None}
        if status == "error":
            logger.error(json.dumps(entry))
        else:
            logger.info(json.dumps(entry))
        span.set_attribute(f"log.{operation_type}.status", status)
        if error_code:
            span.set_status(Status(StatusCode.ERROR))
            span.set_attribute("error.code", error_code)
