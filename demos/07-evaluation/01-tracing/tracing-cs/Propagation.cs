using System.Diagnostics;
using System.Net;

static class Propagation
{
    public static ActivityContext Extract(WebHeaderCollection headers) =>
        ActivityContext.TryParse(headers["traceparent"], headers["tracestate"], out ActivityContext context) ? context : default;
}
