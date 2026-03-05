# Prego Technical Exercise — Stripe → Prego Mapper (C# / .NET 8)

## Active working time

~ 3 hours.
Spent the first 2 hours to understand and build a plan, got stuck a bit on the missing CSV file (was a bit unclear from the instruction) and on understanding Stripe's API.
The rest of the time is for building the program and debugging it

## Overview

This program reads a folder of Stripe-like `*.json` objects and produces **`output.json`** in the required Prego schema:

{
"transactions": [],
"disputes": [],
"refunds": [],
"payouts": []
}

Key goals addressed:

- Enum discipline (only allowed output values; otherwise `not_available`)
- Payment lifecycle understanding (multiple charge snapshots merged deterministically)
- Deterministic ID design
- Clear, maintainable mapping code (one mapper per Stripe object type)

## How to run

From the project folder:

```powershell
dotnet build
dotnet run --project .\PregoStripeMapper.csproj -- --input-dir "." --output output.json
```

- `--input-dir` : folder containing the input `*.json` files (defaults to `.`)
- `--output` : output file path (defaults to `output.json`)

## Inputs

The program expects a directory of Stripe-like JSON objects (each file is a single JSON object) with a top-level `"object"` field, e.g.:

- `"charge"`
- `"dispute"`
- `"refund"`
- `"payout"`

Unsupported object types are ignored.

## Output

The program writes **`output.json`** with:

- `transactions[]`
- `disputes[]`
- `refunds[]`
- `payouts[]`

Arrays are sorted deterministically by their ID key for stable diffs:

- `transaction_id`, `dispute_id`, `refund_id`, `payout_id`

## Enum discipline

The exercise requires: **only values defined in the provided mapping material may be used**.  
This solution enforces that by:

- Centralizing allowed values and canonicalization in `Utils/PregoSchema.cs`
- Mapping all unknown/unreliable values to `not_available`

## Multiple lifecycle snapshots (same underlying charge)

Some sample files represent different lifecycle stages of the same charge ID (e.g., authorized → captured).  
The mapper merges charge snapshots by `transaction_id` and chooses the “most advanced” status using a rank:

- `paid` > failure states > `authorized` > unknown

When a newer snapshot wins, any fields that are still `not_available` are backfilled from the previous best snapshot.

## Assumptions

- Each input file is a single JSON object with a top-level `"object"` field.
- If an enum cannot be confidently inferred, the output uses `not_available`.
- Unsupported Stripe object types are ignored.
- Input files are in one directory (non-recursive).

## What I would improve with more time

- I would create a seperate function that handles the parsing of the input information in a more general way.
  currently im relying on the Stripe format and the existence of the "Object" field, but i would love to not be depending on that.
  Also, in a seperate function it's easy to add more data type that we want to work with without changing the code in the rest of the program (makes the process more modular and fexible).

- Currently, im skipping a lot of unknown types of objects im not familiar with.
  In my opinion its better to save the new types in a database and review it - So if I see that my system doesn't support frequently used objects I will know and I can decide if I want to modify my system to support that.
  Also its a better method to keep and analyze the data for future versions and improvements.
