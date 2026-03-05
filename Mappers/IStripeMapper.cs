using System.Text.Json.Nodes;

namespace PregoStripeMapper.Mappers;

public interface IStripeMapper
{
    string StripeObjectType { get; }
    string OutputKey { get; }
    JsonObject Map(JsonObject stripeObj);
}
