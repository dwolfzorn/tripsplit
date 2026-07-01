using System.Text.Json.Nodes;
using TripSplit.Shared.Import;

namespace TripSplit.Tests;

public class TripImporterTests
{
    private static JsonNode LoadFixture() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "example-trip.json")))!;

    [Fact]
    public void ParseTripImport_LoadsExampleTrip_WithNoSkippedRows()
    {
        var result = TripImporter.ParseTripImport(LoadFixture());

        Assert.Equal(0, result.Skipped.Total);
        Assert.Equal(3, result.Data.ColCities.Count);
        Assert.Equal(4, result.Data.Attendees.Count);
        Assert.Equal(10, result.Data.Expenses.Count);
        Assert.Single(result.Data.Tags);
    }

    [Fact]
    public void ParseTripImport_RejectsUnsupportedSchemaVersion()
    {
        var raw = new JsonObject { ["schemaVersion"] = 99 };

        var ex = Assert.Throws<InvalidOperationException>(() => TripImporter.ParseTripImport(raw));
        Assert.Contains("Unsupported file version", ex.Message);
    }

    [Fact]
    public void ParseTripImport_MigratesLegacyV2NameReferences()
    {
        var raw = new JsonObject
        {
            ["schemaVersion"] = 2,
            ["colCities"] = new JsonArray { new JsonObject { ["city"] = "New York, NY", ["index"] = 100 } },
            ["attendees"] = new JsonArray
            {
                new JsonObject { ["name"] = "Alex", ["income"] = 95000, ["city"] = "New York, NY" },
                new JsonObject { ["name"] = "Brianna", ["income"] = 78000, ["city"] = "New York, NY" }
            },
            ["tags"] = new JsonArray
            {
                new JsonObject { ["id"] = 1, ["name"] = "Alcohol", ["members"] = new JsonArray { "Alex" } }
            },
            ["expenses"] = new JsonArray
            {
                new JsonObject { ["item"] = "Bar tab", ["cost"] = 100, ["purchaser"] = "Brianna", ["tagIds"] = new JsonArray { 1 } }
            }
        };

        var result = TripImporter.ParseTripImport(raw);

        Assert.Equal(0, result.Skipped.Total);
        var alex = Assert.Single(result.Data.Attendees, a => a.Name == "Alex");
        var brianna = Assert.Single(result.Data.Attendees, a => a.Name == "Brianna");
        Assert.Equal([alex.Id], result.Data.Tags[0].MemberIds);
        Assert.Equal(brianna.Id, result.Data.Expenses[0].PurchaserId);
        Assert.Equal(TripSplit.Shared.Models.TripSchema.CurrentVersion, result.Data.SchemaVersion);
    }

    [Fact]
    public void ParseTripImport_SkipsRowsMissingRequiredFields()
    {
        var raw = new JsonObject
        {
            ["schemaVersion"] = 3,
            ["attendees"] = new JsonArray
            {
                new JsonObject { ["name"] = "Alex", ["income"] = 95000, ["city"] = "New York, NY" },
                new JsonObject { ["income"] = 50000 } // missing name -> skipped
            }
        };

        var result = TripImporter.ParseTripImport(raw);

        Assert.Equal(1, result.Skipped.Attendees);
        Assert.Single(result.Data.Attendees);
    }

    [Fact]
    public void ParseTripImport_SkipsRowWithWrongJsonTypeInsteadOfThrowing()
    {
        var raw = new JsonObject
        {
            ["schemaVersion"] = 3,
            ["attendees"] = new JsonArray
            {
                new JsonObject { ["name"] = "Alex", ["income"] = 95000, ["city"] = "New York, NY" },
                new JsonObject { ["name"] = 42, ["income"] = 50000 } // name is a number, not a string -> skipped, not a throw
            }
        };

        var result = TripImporter.ParseTripImport(raw);

        Assert.Equal(1, result.Skipped.Attendees);
        Assert.Single(result.Data.Attendees);
    }

    [Fact]
    public void ParseTripImport_RejectsNonIntegerTagId()
    {
        var raw = new JsonObject
        {
            ["schemaVersion"] = 3,
            ["tags"] = new JsonArray
            {
                new JsonObject { ["id"] = 1.5, ["name"] = "Alcohol", ["memberIds"] = new JsonArray() }
            }
        };

        var result = TripImporter.ParseTripImport(raw);

        Assert.Equal(1, result.Skipped.Tags);
        Assert.Empty(result.Data.Tags);
    }
}
