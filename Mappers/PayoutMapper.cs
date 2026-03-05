using System.Text.Json.Nodes;
using PregoStripeMapper.Utils;

namespace PregoStripeMapper.Mappers;

public sealed class PayoutMapper : IStripeMapper
{
    public string StripeObjectType => "payout";
    public string OutputKey => "payouts";

    public JsonObject Map(JsonObject payout)
    {
        var id = PregoSchema.CanonicalId(payout.GetString("id"));

        var stripeStatus = payout.GetString("status");
        var pregoStatus = stripeStatus switch
        {
            "paid" => "paid",
            "pending" or "in_transit" => "pending",
            "canceled" => "canceled",
            _ => PregoSchema.NotAvailable,
        };
        pregoStatus = PregoSchema.CanonicalEnum(pregoStatus, PregoSchema.PayoutStatuses);

        return new JsonObject
        {
            ["payout_id"] = id,
            ["prego_payout_status"] = pregoStatus,
        };
    }
}