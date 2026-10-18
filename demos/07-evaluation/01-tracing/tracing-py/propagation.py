from opentelemetry.trace.propagation.tracecontext import TraceContextTextMapPropagator

propagator = TraceContextTextMapPropagator()


def inject_headers():
    headers = {}
    propagator.inject(headers)
    return headers


def extract_context(headers):
    return propagator.extract({key.lower(): value for key, value in headers.items()})
