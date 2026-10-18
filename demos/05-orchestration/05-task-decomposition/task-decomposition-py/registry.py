AGENT_REGISTRY = {
    "financial_data_retriever": {
        "capabilities": ["retrieve stock prices", "get financial statements", "query market data APIs"],
        "latency_estimate_sec": 2,
    },
    "sec_filing_analyzer": {
        "capabilities": ["parse SEC filings", "extract financial metrics", "analyze MD&A sections"],
        "latency_estimate_sec": 15,
    },
    "economic_indicator_synthesizer": {
        "capabilities": ["analyze GDP trends", "assess interest rate impacts", "evaluate sector performance"],
        "latency_estimate_sec": 8,
    },
    "investment_thesis_generator": {
        "capabilities": ["synthesize research findings", "generate buy/hold/sell recommendations", "compose analyst reports"],
        "latency_estimate_sec": 12,
    },
}

FILING_TEXT = (
    "Fabrikam Robotics FY2025: revenue 250 million USD, up from 43 million three years ago. "
    "Operating loss of 30 million USD. Total debt 60 million USD against 200 million USD of equity."
)
