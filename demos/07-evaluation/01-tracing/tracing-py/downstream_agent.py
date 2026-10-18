import json
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from opentelemetry import trace
from opentelemetry.trace import SpanKind
from propagation import extract_context
from structured_log import StructuredLogger

tracer = trace.get_tracer("downstream-agents")


def start_agent_service(openai_client, agents):
    class Handler(BaseHTTPRequestHandler):
        def do_POST(self):
            agent_name = self.path.strip("/")
            body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
            parent_context = extract_context(self.headers)
            with tracer.start_as_current_span(f"invoke_agent {agent_name}", context=parent_context, kind=SpanKind.SERVER) as span:
                span.set_attribute("gen_ai.operation.name", "invoke_agent")
                span.set_attribute("gen_ai.agent.name", agent_name)
                if agent_name not in agents:
                    StructuredLogger(agent_name, "0").log("AGENT_CALL", "error", error_code="agent_not_found")
                    self.respond(404, {"error": "agent_not_found"})
                    return
                started = time.perf_counter()
                with tracer.start_as_current_span("chat") as llm_span:
                    response = openai_client.responses.create(
                        input=body["input"],
                        extra_body={"agent_reference": {"name": agent_name, "version": agents[agent_name], "type": "agent_reference"}},
                    )
                    latency_ms = int((time.perf_counter() - started) * 1000)
                    llm_span.set_attribute("gen_ai.operation.name", "chat")
                    llm_span.set_attribute("gen_ai.agent.name", agent_name)
                    llm_span.set_attribute("gen_ai.usage.input_tokens", response.usage.input_tokens)
                    llm_span.set_attribute("gen_ai.usage.output_tokens", response.usage.output_tokens)
                StructuredLogger(agent_name, agents[agent_name]).log(
                    "LLM_CALL",
                    "success",
                    session_id=body.get("session_id"),
                    customer_tier=body.get("customer_tier"),
                    input_token_count=response.usage.input_tokens,
                    output_token_count=response.usage.output_tokens,
                    latency_ms=latency_ms,
                )
                self.respond(200, {"answer": response.output_text})

        def respond(self, status, payload):
            data = json.dumps(payload).encode()
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)

        def log_message(self, format, *args):
            pass

    server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server
