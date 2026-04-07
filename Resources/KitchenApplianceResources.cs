using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace EverythingServer.Resources;

/// <summary>
/// Provides MCP resources exposing a kitchen appliance catalogue as structured table data.
/// </summary>
[McpServerResourceType]
public class KitchenApplianceResources
{
    private static readonly IReadOnlyList<KitchenAppliance> _appliances =
    [
        new(1,  "Stand Mixer",          "Food Preparation", 325,  649.99m,  "KitchenAid",  true),
        new(2,  "Air Fryer",            "Cooking",          1750, 129.99m,  "Ninja",       true),
        new(3,  "Espresso Machine",     "Beverages",        1450, 499.99m,  "De'Longhi",   true),
        new(4,  "Microwave Oven",       "Cooking",          1200, 89.99m,   "Panasonic",   true),
        new(5,  "Slow Cooker",          "Cooking",          240,  49.99m,   "Crockpot",    true),
        new(6,  "Blender",              "Food Preparation", 1200, 79.99m,   "Vitamix",     true),
        new(7,  "Toaster",              "Cooking",          900,  39.99m,   "Breville",    false),
        new(8,  "Food Processor",       "Food Preparation", 750,  119.99m,  "Cuisinart",   true),
        new(9,  "Electric Kettle",      "Beverages",        3000, 34.99m,   "KitchenAid",  true),
        new(10, "Bread Maker",          "Food Preparation", 550,  89.99m,   "Panasonic",   false),
    ];

    /// <summary>
    /// Returns the full kitchen appliance catalogue as a JSON table.
    /// </summary>
    [McpServerResource(
        UriTemplate = "kitchen://appliances/all",
        Name = "kitchen-appliances",
        Title = "Kitchen Appliance Catalogue",
        MimeType = "application/json",
        IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Pot%20of%20food/Flat/pot_of_food_flat.svg")]
    [Description("Returns the full 10-item kitchen appliance catalogue as JSON. Fields: id, name, category, powerWatts, priceGbp, brand, hasDigitalControls.")]
    public static string GetAllAppliances()
        => JsonSerializer.Serialize(
            new { appliances = _appliances },
            new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Returns a single kitchen appliance by its ID.
    /// </summary>
    [McpServerResource(
        UriTemplate = "kitchen://appliances/{id}",
        Name = "kitchen-appliance",
        Title = "Kitchen Appliance",
        MimeType = "application/json",
        IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Pot%20of%20food/Flat/pot_of_food_flat.svg")]
    [Description("Returns a single kitchen appliance by ID as JSON. Fields: id, name, category, powerWatts, priceGbp, brand, hasDigitalControls.")]
    public static string GetApplianceById([Description("Appliance ID, integer 1–10")] int id)
    {
        var appliance = _appliances.FirstOrDefault(a => a.Id == id)
            ?? throw new KeyNotFoundException($"No appliance found with id {id}");

        return JsonSerializer.Serialize(appliance, new JsonSerializerOptions { WriteIndented = true });
    }
}

/// <summary>
/// Represents a single row in the kitchen appliance catalogue.
/// </summary>
public record KitchenAppliance(
    int Id,
    string Name,
    string Category,
    int PowerWatts,
    decimal PriceGbp,
    string Brand,
    bool HasDigitalControls);
