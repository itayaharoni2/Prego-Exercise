using System.Text.Json;
using System.Text.Json.Nodes;
using PregoStripeMapper.Mappers;
using PregoStripeMapper.Utils;

namespace PregoStripeMapper;

// to run: dotnet run -- --input-dir "Prego Technical Exercise" --output output.json
public static class Program
{
    public static int Main(string[] args) // Main returns exit code: 0=ok, non-zero=error
    {
        // Parse CLI args (input folder + output file)
        var (inputDir, outputPath) = ParseArgs(args);

        // Validate input directory. if not found, exit code 2.
        if (!Directory.Exists(inputDir))
        {
            Console.Error.WriteLine($"Input directory not found: {inputDir}");
            return 2;
        }

        // Build mapper registry (object type -> mapper)
        var registry = BuildRegistry();

        // Load all JSON objects from folder
        var stripeObjects = LoadStripeObjects(inputDir);

        // Separate charges from other objects
        var charges = new List<JsonObject>();
        var nonCharges = new List<JsonObject>();

        // Loop over every parsed JSON object
        foreach (var obj in stripeObjects)                             
        {
            var objectType = obj.GetString("object");
            if (objectType == "charge") charges.Add(obj);
            else nonCharges.Add(obj);
        }

        // Create output output skeleton
        var output = NewOutputSkeleton();

        // Map + merge charge snapshots inside ChargeMapper
        var mergedTransactions = ChargeMapper.MapAndMergeMany(charges);
        foreach (var tx in mergedTransactions)
            ((JsonArray)output["transactions"]!).Add(tx);

        // Map all remaining supported object types with registry
        foreach (var obj in nonCharges)
        {
            var objectType = obj.GetString("object");
            if (string.IsNullOrWhiteSpace(objectType)) continue;

            if (!registry.TryGetValue(objectType!, out var mapper))
                continue;

            var mapped = mapper.Map(obj);
            ((JsonArray)output[mapper.OutputKey]!).Add(mapped);
        }

        // Stable sort arrays for deterministic output
        output.SortArrayByKey("transactions", "transaction_id");
        output.SortArrayByKey("disputes", "dispute_id");
        output.SortArrayByKey("refunds", "refund_id");
        output.SortArrayByKey("payouts", "payout_id");

        // Write output.json
        WriteOutput(output, outputPath);

        Console.WriteLine($"Wrote {outputPath}");  // Print success message
        return 0;                                  // Exit code 0 = success
    }

    // -------------------- Helpers --------------------

    // Parse CLI args
    private static (string InputDir, string OutputPath) ParseArgs(string[] args)
    {
        var inputDir = ".";                                            // Default: current folder
        var outputPath = "output.json";                                // Default output file name

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input-dir" && i + 1 < args.Length)
                inputDir = args[++i]; 
            else if (args[i] == "--output" && i + 1 < args.Length)
                outputPath = args[++i];
        }

        return (inputDir, outputPath);
    }

    // Build a registry mapping Stripe object types -> their mapper implementation.
    // Adding a new type = only need to add a new mapper instance here
    private static Dictionary<string, IStripeMapper> BuildRegistry()
    {
        IStripeMapper[] mappers =
        [
            new DisputeMapper(),
            new RefundMapper(),
            new PayoutMapper(),
        ];

        return mappers.ToDictionary(m => m.StripeObjectType, m => m);
    }

    // Load and parse all json files in a folder into JsonObjects
    private static List<JsonObject> LoadStripeObjects(string inputDir)
    {
        var list = new List<JsonObject>();

        foreach (var path in Directory.EnumerateFiles(inputDir, "*.json").OrderBy(p => p))
        {
            try
            {
                var node = JsonNode.Parse(File.ReadAllText(path));
                if (node is JsonObject obj)
                    list.Add(obj);
            }
            catch
            {
                // Ignore invalid JSON
            }
        }

        return list;
    }

    // Create the exact output structure required
    private static JsonObject NewOutputSkeleton()
    {
        return new JsonObject
        {
            ["transactions"] = new JsonArray(),
            ["disputes"] = new JsonArray(),
            ["refunds"] = new JsonArray(),
            ["payouts"] = new JsonArray(),
        };
    }

    // Serialize and write output JSON to disk
    private static void WriteOutput(JsonObject output, string outputPath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outputPath, output.ToJsonString(options));
    }
}