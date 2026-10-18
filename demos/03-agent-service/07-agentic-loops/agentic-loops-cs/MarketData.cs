using System.Text.Json;

static class MarketData
{
    static readonly Dictionary<string, object> Data = new()
    {
        ["MSFT"] = new { price = 428.5, pe_ratio = 35.2, revenue_growth_3yr = 0.14, operating_margin = 0.45, debt_to_equity = 0.33, sector = "Technology" },
        ["GOOG"] = new { price = 176.2, pe_ratio = 24.8, revenue_growth_3yr = 0.12, operating_margin = 0.32, debt_to_equity = 0.10, sector = "Technology" },
        ["NVDA"] = new { price = 121.9, pe_ratio = 62.4, revenue_growth_3yr = 0.78, operating_margin = 0.62, debt_to_equity = 0.25, sector = "Technology" },
    };

    public static string Get(string ticker) =>
        JsonSerializer.Serialize(Data.TryGetValue(ticker.ToUpperInvariant(), out object? entry) ? entry : new { error = $"unknown ticker {ticker}" });
}
