import hmac
import os
import uvicorn
from dotenv import load_dotenv
from mcp.server.fastmcp import FastMCP
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import JSONResponse
from backend import DrugInteractionInstance, PermanentError, TransientError
from registry import ToolRegistry, ToolRouter
from resilience import CircuitBreaker, CircuitBreakerConfig, CircuitBreakerOpenError, mcp_retry_with_backoff

load_dotenv()
API_KEY = os.getenv("MCP_API_KEY")
PORT = int(os.getenv("MCP_PORT", "8765"))

east = DrugInteractionInstance("drugs-east", "1.0.0", 0.15)
west = DrugInteractionInstance("drugs-west", "1.0.0", 0.05)
canary = DrugInteractionInstance("drugs-canary", "1.1.0", 0.02)

registry = ToolRegistry()
registry.register_instance("lookup_drug_interaction", "drugs-east", "1.0.0", east)
registry.register_instance("lookup_drug_interaction", "drugs-west", "1.0.0", west)
registry.register_instance("lookup_drug_interaction", "drugs-canary", "1.1.0", canary, health_status="unhealthy")
router = ToolRouter(registry)
circuit = CircuitBreaker(CircuitBreakerConfig(failure_threshold=3, recovery_timeout=10.0, success_threshold=2))

mcp = FastMCP("northwind-drug-tools")


@mcp_retry_with_backoff(max_attempts=3, base_delay_seconds=0.2, max_delay_seconds=2)
def call_backend(drug_a, drug_b):
    return router.resolve_and_call("lookup_drug_interaction", drug_a=drug_a, drug_b=drug_b)


@mcp.tool()
def lookup_drug_interaction(drug_a: str, drug_b: str) -> dict:
    """Check for interactions between two medications."""
    try:
        return circuit.call(call_backend, drug_a, drug_b)
    except CircuitBreakerOpenError as error:
        return {
            "status": "unavailable",
            "reason": "service_circuit_open",
            "fallback": "consult_pharmacist",
            "retry_after_seconds": round(circuit.time_until_recovery(), 1),
            "message": str(error),
        }
    except TransientError as error:
        return {"status": "unavailable", "reason": "retries_exhausted", "fallback": "consult_pharmacist", "message": str(error)}
    except PermanentError as error:
        return {"status": "error", "error_type": "invalid_input", "message": str(error)}


@mcp.custom_route("/health", methods=["GET"])
async def health(request: Request):
    instances = {i.endpoint: i.health_status for i in registry.instances("lookup_drug_interaction")}
    status = "degraded" if circuit.state.value == "open" else "healthy"
    return JSONResponse({"status": status, "instances": instances, "circuit": circuit.state.value})


@mcp.custom_route("/metrics", methods=["GET"])
async def metrics(request: Request):
    counts = {}
    for decision in router.decisions:
        key = f"{decision['selected_endpoint']} ({decision['routing_strategy']})"
        counts[key] = counts.get(key, 0) + 1
    p95 = {i.endpoint: round(i.p95_latency_ms, 1) for i in registry.instances("lookup_drug_interaction")}
    return JSONResponse({"routing": counts, "p95_latency_ms": p95, "circuit": circuit.state.value})


@mcp.custom_route("/admin/outage", methods=["POST"])
async def outage(request: Request):
    on = request.query_params.get("on") == "1"
    east.outage = on
    west.outage = on
    return JSONResponse({"outage": on})


@mcp.custom_route("/admin/canary", methods=["POST"])
async def canary_weights(request: Request):
    percent = float(request.query_params.get("percent", "0"))
    registry.update_health("lookup_drug_interaction", "drugs-canary", "healthy" if percent > 0 else "unhealthy")
    if percent > 0:
        router.set_weights("lookup_drug_interaction", {"drugs-east": 0, "drugs-west": 100 - percent, "drugs-canary": percent})
    else:
        router.weights.pop("lookup_drug_interaction", None)
    return JSONResponse({"canary_percent": percent})


class ApiKeyMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request, call_next):
        if not hmac.compare_digest(request.headers.get("x-api-key", "").encode(), API_KEY.encode()):
            return JSONResponse({"error": "unauthorized"}, status_code=401)
        return await call_next(request)


app = mcp.streamable_http_app()
app.add_middleware(ApiKeyMiddleware)

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=PORT, log_level="warning")
