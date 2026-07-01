using System.Globalization;
using System.Text.Json.Nodes;
using TripSplit.Shared.Models;

namespace TripSplit.Shared.Import;

public record ImportSkipCounts(int ColCities, int Attendees, int Expenses, int Tags)
{
    public int Total => ColCities + Attendees + Expenses + Tags;
}

public record ImportResult(TripDto Data, ImportSkipCounts Skipped);

// Port of parseTripImport + the v1->v3 migration from the original index.html.
// Validates and normalizes a parsed import payload; rows missing required
// fields are dropped (and counted) rather than failing the whole import.
//   v1: no exclusion tags.
//   v2: added exclusion tags; expenses/tags referenced attendees by name
//       (`purchaser`, `tag.members`).
//   v3: attendees have a stable `id`; expenses/tags reference them by id
//       (`purchaserId`, `tag.memberIds`).
// Older files are migrated to the current shape on import.
public static class TripImporter
{
    public static ImportResult ParseTripImport(JsonNode? raw)
    {
        if (raw is not JsonObject root)
            throw new InvalidOperationException("File is not a valid TripSplit export.");

        var schemaVersion = ParseLoose<int>(root["schemaVersion"]);
        if (schemaVersion is null || !TripSchema.SupportedVersions.Contains(schemaVersion.Value))
            throw new InvalidOperationException($"Unsupported file version (got {schemaVersion?.ToString() ?? "null"}).");

        var skippedColCities = 0;
        var colCities = new List<ColCity>();
        foreach (var node in AsArray(root["colCities"]))
        {
            var city = GetStringLoose(node?["city"]);
            var index = ParseLoose<double>(node?["index"]);
            if (string.IsNullOrWhiteSpace(city) || index is null) { skippedColCities++; continue; }
            colCities.Add(new ColCity { City = city, Index = index.Value });
        }

        // v1/v2 files have no attendee ids - assign fresh sequential ids on
        // import (preserving real ids from v3 files where present).
        var skippedAttendees = 0;
        var attendees = new List<Attendee>();
        var nextId = 1;
        foreach (var node in AsArray(root["attendees"]))
        {
            var name = GetStringLoose(node?["name"]);
            if (name is null) { skippedAttendees++; continue; }
            var id = ParseLoose<int>(node?["id"]) ?? nextId++;
            attendees.Add(new Attendee
            {
                Id = id,
                Name = name,
                Income = ParseLoose<decimal>(node?["income"]),
                City = GetStringLoose(node?["city"]) ?? ""
            });
        }
        foreach (var a in attendees) nextId = Math.Max(nextId, a.Id + 1);

        int? ResolveNameToId(string name) => attendees
            .FirstOrDefault(a => string.Equals(a.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;

        // v1/v2 tags reference members by name (`members`); v3 references by
        // id (`memberIds`).
        var skippedTags = 0;
        var tags = new List<ExclusionTag>();
        foreach (var node in AsArray(root["tags"]))
        {
            var name = GetStringLoose(node?["name"]);
            var id = ParseLoose<int>(node?["id"]);
            if (name is null || id is null) { skippedTags++; continue; }

            List<int> memberIds;
            if (node?["memberIds"] is JsonArray memberIdArr)
            {
                memberIds = memberIdArr.Select(ParseLoose<int>).Where(v => v is not null).Select(v => v!.Value).ToList();
            }
            else if (node?["members"] is JsonArray memberArr)
            {
                memberIds = memberArr
                    .Select(GetStringLoose)
                    .Where(n => n is not null)
                    .Select(n => ResolveNameToId(n!))
                    .Where(resolvedId => resolvedId is not null)
                    .Select(resolvedId => resolvedId!.Value)
                    .ToList();
            }
            else
            {
                memberIds = [];
            }

            tags.Add(new ExclusionTag { Id = id.Value, Name = name, MemberIds = memberIds });
        }

        var validTagIds = tags.Select(t => t.Id).ToHashSet();

        // v1/v2 expenses reference a purchaser by name (`purchaser`); v3 by
        // id (`purchaserId`).
        var skippedExpenses = 0;
        var expenses = new List<Expense>();
        foreach (var node in AsArray(root["expenses"]))
        {
            var item = GetStringLoose(node?["item"]);
            if (item is null) { skippedExpenses++; continue; }

            var purchaserId = ParseLoose<int>(node?["purchaserId"]);
            if (purchaserId is null && GetStringLoose(node?["purchaser"]) is { Length: > 0 } purchaserName)
            {
                purchaserId = ResolveNameToId(purchaserName);
            }

            var tagIds = node?["tagIds"] is JsonArray tagIdArr
                ? tagIdArr.Select(ParseLoose<int>).Where(v => v is not null).Select(v => v!.Value).Where(validTagIds.Contains).ToList()
                : [];

            expenses.Add(new Expense
            {
                Item = item,
                Cost = ParseLoose<decimal>(node?["cost"]),
                PurchaserId = purchaserId,
                TagIds = tagIds
            });
        }

        var data = new TripDto
        {
            SchemaVersion = TripSchema.CurrentVersion,
            ColCities = colCities,
            Attendees = attendees,
            Expenses = expenses,
            Tags = tags
        };

        return new ImportResult(data, new ImportSkipCounts(skippedColCities, skippedAttendees, skippedExpenses, skippedTags));
    }

    private static IEnumerable<JsonNode?> AsArray(JsonNode? node) => node as JsonArray ?? [];

    // Reads a string field without throwing when the JSON value is present
    // but isn't a string (e.g. a hand-edited file with a numeric `name`) -
    // treated the same as a missing field, so the row is skipped-and-counted
    // rather than aborting the whole import.
    private static string? GetStringLoose(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    // JsonValue.TryGetValue<T> only succeeds for T matching (or directly
    // convertible from) the node's underlying storage type - e.g. a node
    // built from a C# `int` literal fails TryGetValue<double>(). Falling back
    // to the node's JSON text via T's own IParsable<T>.TryParse covers that
    // case and numeric values that arrived as JSON strings, while still
    // rejecting values that aren't valid for T - e.g. int.TryParse("1.5")
    // correctly fails, so a non-integer id is treated as invalid rather than
    // silently truncated.
    private static T? ParseLoose<T>(JsonNode? node) where T : struct, IParsable<T>
    {
        if (node is not JsonValue value) return null;
        if (value.TryGetValue<T>(out var direct)) return direct;
        if (value.TryGetValue<string>(out var s) && T.TryParse(s, CultureInfo.InvariantCulture, out var fromString)) return fromString;
        return T.TryParse(value.ToJsonString(), CultureInfo.InvariantCulture, out var fromJson) ? fromJson : null;
    }
}
