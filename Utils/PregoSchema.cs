using System.Text.Json;
using System.Text.Json.Nodes;

namespace PregoStripeMapper.Utils;

public static class PregoSchema
{
    public const string NotAvailable = "not_available";

    // Allowed enum values (canonical, lowercase)
    public static readonly HashSet<string> TransactionStatuses = new(StringComparer.Ordinal)
    {
        "authorized",
        "cancel_or_refund",
        "canceled",
        "capture",
        "deposit",
        "failed",
        "gateway_rejected",
        "paid",
        "pending",
        "processor_declined",
        NotAvailable,
    };

    // From Assignment examples
    public static readonly HashSet<string> PaymentMethods = new(StringComparer.Ordinal)
    {
        "card",
        "apple pay",
        "paywithgoogle",
        NotAvailable,
    };
    public static readonly HashSet<string> CardTypes = new(StringComparer.Ordinal)
    {
        "debit",
        "credit",
        NotAvailable,
    };
    public static readonly HashSet<string> Networks = new(StringComparer.Ordinal)
    {
        "visa",
        "mastercard",
        "amex",
        "discover",
        "diners",
        "jcb",
        "unionpay",
        NotAvailable,
    };
    public static readonly HashSet<string> DisputeStatuses = new(StringComparer.Ordinal)
    {
        "won",
        "lost",
        "pending",
        NotAvailable,
    };
    public static readonly HashSet<string> DisputeReasons = new(StringComparer.Ordinal)
    {
        "duplicate processing",
        "fraud",
        "stolen card",
        NotAvailable,
    };
    public static readonly HashSet<string> RefundStatuses = new(StringComparer.Ordinal)
    {
        "success",
        "pending",
        "rejected",
        NotAvailable,
    };

    // Assignment does not enumerate refund reasons. best-effort whitelist based on Stripe refund reasons
    public static readonly HashSet<string> RefundReasons = new(StringComparer.Ordinal)
    {
        "duplicate",
        "fraudulent",
        "requested_by_customer",
        "expired_uncaptured",
        NotAvailable,
    };
    public static readonly HashSet<string> PayoutStatuses = new(StringComparer.Ordinal)
    {
        "paid",
        "pending",
        "canceled",
        NotAvailable,
    };

    // Assignment does not enumerate transaction_response values
    // Best-effort whitelist based on common Stripe issuer/decline codes
    public static readonly HashSet<string> TransactionResponses = new(StringComparer.Ordinal)
    {
        // Sample
        "insufficient_funds",

        // Common issuer decline / response codes
        "do_not_honor",
        "generic_decline",
        "fraudulent",
        "lost_card",
        "stolen_card",
        "pickup_card",
        "call_issuer",
        "expired_card",
        "incorrect_cvc",
        "incorrect_number",
        "incorrect_zip",
        "invalid_account",
        "invalid_amount",
        "invalid_cvc",
        "invalid_expiry_month",
        "invalid_expiry_year",
        "invalid_number",
        "not_permitted",
        "processing_error",
        "restricted_card",
        "security_violation",
        "service_not_allowed",
        "transaction_not_allowed",
        "try_again_later",
        "withdrawal_count_limit_exceeded",
        "card_not_supported",
        "currency_not_supported",
        "duplicate_transaction",
        "card_velocity_exceeded",

        NotAvailable,
    };

    // ------ helpers ------

    public static string CanonicalId(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? NotAvailable : raw.Trim();

    public static string CanonicalLower(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? NotAvailable : raw.Trim().ToLowerInvariant();

    public static string CanonicalEnum(string? raw, HashSet<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(raw)) return NotAvailable;
        var s = raw.Trim().ToLowerInvariant();
        return allowed.Contains(s) ? s : NotAvailable;
    }

    public static string Truncate(string value, int maxLen)
        => value.Length <= maxLen ? value : value[..maxLen];

    // Clone a JsonObject defensively
    public static JsonObject CloneObject(JsonObject obj)
        => (JsonObject)JsonNode.Parse(obj.ToJsonString())!;
}