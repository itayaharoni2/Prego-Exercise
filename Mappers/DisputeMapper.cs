using System.Text.Json.Nodes;
using PregoStripeMapper.Utils;

namespace PregoStripeMapper.Mappers;

public sealed class DisputeMapper : IStripeMapper
{
    public string StripeObjectType => "dispute";
    public string OutputKey => "disputes";

    public JsonObject Map(JsonObject dispute)
    {
        var id = PregoSchema.CanonicalId(dispute.GetString("id"));

        var stripeStatus = dispute.GetString("status");
        var pregoStatus = stripeStatus switch
        {
            "won" => "won",
            "lost" => "lost",

            // Stripe uses several "in progress" statuses.
            "needs_response" or "warning_needs_response" or "under_review" => "pending",

            _ => "pending",
        };
        pregoStatus = PregoSchema.CanonicalEnum(pregoStatus, PregoSchema.DisputeStatuses);

        var stripeReason = dispute.GetString("reason");
        var pregoReason = stripeReason switch
        {
            "fraudulent" => "fraud",
            "duplicate" => "duplicate processing",
            "stolen_card" => "stolen card",
            _ => PregoSchema.NotAvailable,
        };
        pregoReason = PregoSchema.CanonicalEnum(pregoReason, PregoSchema.DisputeReasons);

        return new JsonObject
        {
            ["dispute_id"] = id,
            ["prego_dispute_status"] = pregoStatus,
            ["prego_dispute_reason"] = pregoReason,
        };
    }
}