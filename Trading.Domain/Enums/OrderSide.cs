using System.Text.Json.Serialization;

namespace Trading.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderSide
{
    Buy,
    Sell
}