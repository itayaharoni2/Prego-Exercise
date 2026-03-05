using System.Text.Json.Nodes;
using PregoStripeMapper.Utils;

namespace PregoStripeMapper.Mappers;

public sealed class ChargeMapper : IStripeMapper
{
    public string StripeObjectType => "charge";
    public string OutputKey => "transactions";

    // Map ONE Stripe charge object into ONE Prego transaction record
    public JsonObject Map(JsonObject charge)
    {
        var transactionId = PregoSchema.CanonicalId(charge.GetString("id"));

        var pregoStatus = PickStatus(charge);
        pregoStatus = PregoSchema.CanonicalEnum(pregoStatus, PregoSchema.TransactionStatuses);

        var paymentMethod = PickPaymentMethod(charge);
        paymentMethod = PregoSchema.CanonicalEnum(paymentMethod, PregoSchema.PaymentMethods);

        var cardType = PickCardType(charge);
        var network = PickNetwork(charge);

        // Issuer response / decline code
        var response = PickTransactionResponse(charge, pregoStatus);

        return new JsonObject
        {
            ["transaction_id"] = transactionId,
            ["prego_status"] = pregoStatus,
            ["prego_payment_method"] = paymentMethod,
            ["prego_card_type"] = cardType,
            ["prego_transaction_response"] = response,
            ["prego_network"] = network,
        };
    }

    // Map + merge many charge snapshots
    public static List<JsonObject> MapAndMergeMany(IEnumerable<JsonObject> charges)
    {
        var mapper = new ChargeMapper();

        var bestById = new Dictionary<string, JsonObject>();
        var bestRankById = new Dictionary<string, int>();

        foreach (var charge in charges)
        {
            var tx = mapper.Map(charge);
            var tid = tx.GetString("transaction_id");
            if (string.IsNullOrWhiteSpace(tid)) continue;

            var rank = TransactionRank(tx.GetString("prego_status"));

            if (!bestById.ContainsKey(tid))
            {
                bestById[tid] = tx;
                bestRankById[tid] = rank;
                continue;
            }

            // If the new snapshot wins, backfill any not_available fields from the previous best snapshot
            if (rank > bestRankById[tid])
            {
                bestById[tid] = MergeFillNotAvailable(tx, bestById[tid]);
                bestRankById[tid] = rank;
            }
            else
            {
                bestById[tid] = MergeFillNotAvailable(bestById[tid], tx);
            }
        }

        return bestById
            .OrderBy(k => k.Key)
            .Select(k => k.Value)
            .ToList();
    }

    public static int TransactionRank(string? pregoStatus) => pregoStatus switch
    {
        "paid" => 3,
        "failed" or "processor_declined" or "gateway_rejected" => 2,
        "authorized" => 1,
        _ => 0,
    };


    // Keep primary values unless they are missing / not_available, then backfill from fallback
    private static JsonObject MergeFillNotAvailable(JsonObject primary, JsonObject fallback)
    {
        var merged = new JsonObject();

        foreach (var kv in primary)
            merged[kv.Key] = kv.Value?.DeepClone();

        foreach (var kv in fallback)
        {
            var key = kv.Key;
            var fallbackValue = kv.Value;

            if (!merged.ContainsKey(key))
            {
                merged[key] = fallbackValue?.DeepClone();
                continue;
            }

            var currentStr = merged[key]?.ToString();
            var fallbackStr = fallbackValue?.ToString();

            if (string.IsNullOrWhiteSpace(currentStr) || currentStr == PregoSchema.NotAvailable)
            {
                if (!string.IsNullOrWhiteSpace(fallbackStr) && fallbackStr != PregoSchema.NotAvailable)
                    merged[key] = fallbackValue?.DeepClone();
            }
        }

        return merged;
    }

    private static string PickStatus(JsonObject charge)
    {
        var status = charge.GetString("status"); // succeeded / pending / failed
        var captured = charge.GetBool("captured");
        var paid = charge.GetBool("paid");
        var amountCaptured = charge.GetLong("amount_captured") ?? 0;

        var outcomeType = charge.GetPathString("outcome.type"); // authorized / issuer_declined / blocked / invalid
        var networkStatus = charge.GetPathString("outcome.network_status"); // approved_by_network / declined_by_network

        if (status == "failed")
        {
            if (outcomeType == "issuer_declined" || networkStatus == "declined_by_network")
                return "processor_declined";

            if (outcomeType is "blocked" or "invalid")
                return "gateway_rejected";

            return "failed";
        }

        if (captured || paid || amountCaptured > 0)
            return "paid";

        if (status == "pending")
            return "pending";

        if (status == "succeeded" && !captured && !paid)
            return "authorized";

        return PregoSchema.NotAvailable;
    }

    private static string PickPaymentMethod(JsonObject charge)
    {
        var pmdType = charge.GetPathString("payment_method_details.type");
        if (string.IsNullOrWhiteSpace(pmdType))
            return PregoSchema.NotAvailable;

        // only support mappings explicitly required
        if (pmdType != "card")
            return PregoSchema.NotAvailable;

        var walletType = charge.GetPathString("payment_method_details.card.wallet.type");
        if (walletType == "apple_pay") return "apple pay";
        if (walletType is "google_pay" or "android_pay") return "paywithgoogle";

        return "card";
    }

    private static string PickCardType(JsonObject charge)
    {
        var funding = charge.GetPathString("payment_method_details.card.funding");
        return PregoSchema.CanonicalEnum(funding, PregoSchema.CardTypes);
    }

    private static string PickNetwork(JsonObject charge)
    {
        var raw =
            charge.GetPathString("payment_method_details.card.network") ??
            charge.GetPathString("payment_method_details.card.brand");

        var normalized = PregoSchema.CanonicalLower(raw) switch
        {
            "american_express" => "amex",
            _ => PregoSchema.CanonicalLower(raw),
        };

        return PregoSchema.CanonicalEnum(normalized, PregoSchema.Networks);
    }

    private static string PickTransactionResponse(JsonObject charge, string pregoStatus)
    {
        // Only reliable for declines
        var isFailure = pregoStatus is "failed" or "processor_declined" or "gateway_rejected";
        if (!isFailure) return PregoSchema.NotAvailable;

        var raw =
            charge.GetPathString("outcome.reason") ??
            charge.GetString("failure_code");

        return PregoSchema.CanonicalEnum(raw, PregoSchema.TransactionResponses);
    }
}