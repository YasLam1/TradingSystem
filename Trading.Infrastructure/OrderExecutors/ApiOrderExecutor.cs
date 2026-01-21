using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Infrastructure.OrderExecutors;

public sealed class ApiOrderExecutor : IOrderExecutor
{
    private readonly HttpClient _http;

    public ApiOrderExecutor(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Execution> ExecuteOrderAsync(Order order, Quote lastQuote, CancellationToken ct)
    {
        var payload = new
        {
            id = order.Id,
            symbol = order.Symbol,
            side = order.Side.ToString(),
            type = order.Type.ToString(),
            quantity = order.Quantity,
            price = order.Price,
            context = new { bid = lastQuote.Bid, ask = lastQuote.Ask, ts = lastQuote.Timestamp }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload),
                                        System.Text.Encoding.UTF8,
                                        "application/json")
        };

       
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);

        var dto = System.Text.Json.JsonSerializer.Deserialize<OrderExecutionDto>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dto == null)
            throw new InvalidOperationException("Malformed broker response");

        return new Execution
        {
            OrderId = order.Id,
            Symbol = order.Symbol,
            Side = order.Side,
            FillPrice = dto.fillPrice,
            FilledQuantity = dto.filledQuantity,
            Timestamp = dto.timestamp == default ? DateTime.UtcNow : dto.timestamp
        };
    }

    private sealed class OrderExecutionDto
    {
        public decimal fillPrice { get; set; }
        public int filledQuantity { get; set; }
        public DateTime timestamp { get; set; }
    }
}
