using Trading.Domain.Interfaces;

public static class TestingDataFeed
{
    public static async Task Test(IMarketDataFeed feed, string symbol)
    {
        using var cts = new CancellationTokenSource();

        Console.WriteLine($"Streaming {symbol} prices...");
        Console.WriteLine("Press ENTER to stop\n");

        // Stop when user presses Enter
        _ = Task.Run(() =>
        {
            Console.ReadLine();
            cts.Cancel();
        });

        try
        {
            await foreach (var quote in feed.StartPriceStreamAsync(symbol, cts.Token))
            {
                Console.WriteLine(
                    $"{quote.Timestamp:HH:mm:ss} | " +
                    $"{quote.Symbol} | " +
                    $"Price: {quote.LastTradedPrice}"
                );
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nStream stopped.");
        }
    }
}