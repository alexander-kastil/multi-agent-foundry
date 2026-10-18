from opentelemetry.sdk.trace.export import SpanExporter, SpanExportResult
from opentelemetry.trace import StatusCode

SCOPES = ("agent_framework", "orchestrator", "downstream-agents", "opentelemetry.instrumentation.httpx")


class SpanCollector(SpanExporter):
    def __init__(self, latency_threshold_ms=5000):
        self.latency_threshold_ms = latency_threshold_ms
        self.spans = []

    def export(self, spans):
        self.spans.extend(span for span in spans if span.instrumentation_scope.name.startswith(SCOPES))
        return SpanExportResult.SUCCESS

    def shutdown(self):
        pass

    def flagged(self):
        result = []
        for span in self.spans:
            duration_ms = (span.end_time - span.start_time) / 1_000_000
            if span.status.status_code == StatusCode.ERROR or duration_ms > self.latency_threshold_ms:
                result.append(span)
        return result

    def trace_ids(self):
        return {format(span.context.trace_id, "032x") for span in self.spans}

    def waterfall(self):
        lines = []
        ids = {span.context.span_id for span in self.spans}
        children = {}
        for span in sorted(self.spans, key=lambda s: s.start_time):
            parent_id = span.parent.span_id if span.parent is not None and span.parent.span_id in ids else None
            children.setdefault(parent_id, []).append(span)

        def walk(parent_id, level):
            for span in children.get(parent_id, []):
                duration_ms = (span.end_time - span.start_time) / 1_000_000
                trace_id = format(span.context.trace_id, "032x")[:8]
                lines.append(f"{trace_id}  {'  ' * level}{span.name}  {duration_ms:.0f} ms  {span.status.status_code.name}")
                walk(span.context.span_id, level + 1)

        walk(None, 0)
        return "\n".join(lines)
