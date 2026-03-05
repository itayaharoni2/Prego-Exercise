using System.Text.Json.Nodes;
using PregoStripeMapper.Utils;

namespace PregoStripeMapper.Mappers;

public sealed class RefundMapper : IStripeMapper
{
    public string StripeObjectType => "refund";
    public string OutputKey => "refunds";

    public JsonObject Map(JsonObject refund)
    {
        var id = PregoSchema.CanonicalId(refund.GetString("id"));

        var stripeStatus = refund.GetString("status");
        var pregoStatus = stripeStatus switch
        {
            "succeeded" => "success",
            "pending" => "pending",
            "failed" or "canceled" => "rejected",
            _ => PregoSchema.NotAvailable,
        };
        pregoStatus = PregoSchema.CanonicalEnum(pregoStatus, PregoSchema.RefundStatuses);

        var stripeReason = refund.GetString("reason");
        var reason = PregoSchema.CanonicalEnum(stripeReason, PregoSchema.RefundReasons);

        return new JsonObject
        {
            ["refund_id"] = id,
            ["prego_refund_status"] = pregoStatus,
            ["prego_refund_reason"] = reason,
        };
    }
}