using System.Globalization;
using System.Text;
using Trading.Domain.Entities;

public static class CsvQuoteUtils
{
    public static void Save(IEnumerable<Quote> quotes, string filePath)
    {
        StringBuilder sb = new();

        sb.AppendLine("timestamp,close");

        foreach (Quote q in quotes)
        {
            sb.AppendLine(
                $"{q.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                $"{q.Bid.ToString(CultureInfo.InvariantCulture)}");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public static IEnumerable<Quote> Read(string filePath, string symbol)
    {
        var lines = File.ReadAllLines(filePath).Skip(1); // skip header

        foreach (string line in lines)
        {
            var parts = line.Split(',');
            DateTime time = DateTime.Parse(parts[0], CultureInfo.InvariantCulture);
            decimal price = decimal.Parse(parts[1], CultureInfo.InvariantCulture);

            yield return new Quote
            {
                Symbol = symbol,
                Timestamp = time,
                Bid = price,
                Ask = price,
                LastTradedPrice = price,
            };
        }
    }

    public static IEnumerable<Quote> ReadV2(string filePath, string symbol)
    {
        var lines = File.ReadAllLines(filePath).Skip(1); // skip header

        foreach (string line in lines)
        {
            var parts = line.Split(',');
            DateTime time = DateTime.Parse(parts[0], CultureInfo.InvariantCulture);
            decimal price = decimal.Parse(parts[4], CultureInfo.InvariantCulture);

            yield return new Quote
            {
                Symbol = symbol,
                Timestamp = time,
                Bid = price,
                Ask = price,
                LastTradedPrice = price
            };
        }
    }
}
