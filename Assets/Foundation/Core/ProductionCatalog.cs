using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    public enum ProductionCategory
    {
        MetalWeapon, MetalArmor, Shield, RangedWeapon, MagicWeapon, LeatherArmor, ClothArmor, Offhand, Food
    }

    public sealed class ProductionRecipe
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string SkillId { get; }
        public ProductionCategory Category { get; }
        public string OutputId { get; }
        public string OutputName { get; }
        public long OutputQuantity { get; }
        public ReadOnlyDictionary<string, long> Inputs { get; }
        public int RequiredLevel { get; }
        public long DurationTicks { get; }
        public decimal? XpReward { get; }
        public bool IsDevelopmentOnly { get; }
        public string Provenance { get; }
        public bool IsBalanceAssumption { get; }

        public ProductionRecipe(string id, string name, string skillId, ProductionCategory category,
            string outputId, string outputName, long outputQuantity, IEnumerable<KeyValuePair<string, long>> inputs,
            int requiredLevel, long durationTicks, decimal? xpReward, bool developmentOnly, string provenance, bool balanceAssumption = false)
        {
            if (!ValidId(id) || !ValidId(outputId) || string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(outputName) || string.IsNullOrWhiteSpace(provenance))
                throw new ArgumentException("Stable IDs, labels and source provenance required.");
            bool allowed = skillId == "smithing" && (category == ProductionCategory.MetalWeapon ||
                category == ProductionCategory.MetalArmor || category == ProductionCategory.Shield) ||
                skillId == "crafting" && (category == ProductionCategory.RangedWeapon ||
                category == ProductionCategory.MagicWeapon || category == ProductionCategory.LeatherArmor ||
                category == ProductionCategory.ClothArmor || category == ProductionCategory.Offhand) ||
                skillId == "cooking" && category == ProductionCategory.Food;
            if (developmentOnly && balanceAssumption) throw new ArgumentException("Fixtures cannot be live balance assumptions.");
            if (!allowed) throw new ArgumentException("Category is not supported by this production skill.");
            if (outputQuantity <= 0 || requiredLevel < 1 || requiredLevel > 99 || durationTicks <= 0 || xpReward < 0)
                throw new ArgumentOutOfRangeException(nameof(outputQuantity), "Invalid recipe quantity, level, duration or XP.");
            if (developmentOnly && (!id.StartsWith("dev-", StringComparison.Ordinal) ||
                !outputId.StartsWith("dev-", StringComparison.Ordinal) || !name.StartsWith("[DEV]", StringComparison.Ordinal) ||
                !outputName.StartsWith("[DEV]", StringComparison.Ordinal)))
                throw new ArgumentException("Development recipes and products require explicit dev IDs and labels.");
            if (!developmentOnly && (id.StartsWith("dev-", StringComparison.Ordinal) ||
                outputId.StartsWith("dev-", StringComparison.Ordinal) || name.StartsWith("[DEV]", StringComparison.Ordinal) ||
                outputName.StartsWith("[DEV]", StringComparison.Ordinal)))
                throw new ArgumentException("Development identifiers cannot be marked approved.");
            var copy = new Dictionary<string, long>();
            foreach (var input in inputs ?? throw new ArgumentNullException(nameof(inputs)))
            {
                if (!ValidId(input.Key) || input.Value <= 0 || input.Key == outputId || copy.ContainsKey(input.Key))
                    throw new ArgumentException("Inputs need distinct item IDs and positive quantities; self-input is invalid.");
                copy.Add(input.Key, input.Value);
            }
            if (copy.Count == 0) throw new ArgumentException("Production must consume ingredients.");
            Id = id; DisplayName = name; SkillId = skillId; Category = category;
            OutputId = outputId; OutputName = outputName; OutputQuantity = outputQuantity;
            Inputs = new ReadOnlyDictionary<string, long>(copy);
            RequiredLevel = requiredLevel; DurationTicks = durationTicks; XpReward = xpReward;
            IsDevelopmentOnly = developmentOnly; Provenance = provenance; IsBalanceAssumption = balanceAssumption;
        }

        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) &&
            id.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-');

        internal ActivityDefinition Activity() => new ActivityDefinition(Id, DisplayName, SkillId, true,
            RequiredLevel, DurationTicks, OutputId, XpReward);
    }

    public sealed class ProductionCatalog
    {
        public ReadOnlyCollection<ProductionRecipe> Recipes { get; }
        public ReadOnlyDictionary<string, string> ItemNames { get; }
        public bool HasDevelopmentRecipes => Recipes.Any(r => r.IsDevelopmentOnly);

        public ProductionCatalog(IEnumerable<ProductionRecipe> recipes, IEnumerable<ResourceDefinition> resources,
            bool allowDevelopment = false)
        {
            var list = recipes?.ToList() ?? throw new ArgumentNullException(nameof(recipes));
            var raw = resources?.ToList() ?? throw new ArgumentNullException(nameof(resources));
            if (list.Any(r => r == null) || list.Select(r => r.Id).Distinct().Count() != list.Count)
                throw new ArgumentException("Duplicate/null recipe definitions.");
            if (!allowDevelopment && list.Any(r => r.IsDevelopmentOnly))
                throw new ArgumentException("Development recipes require explicit opt-in.");
            var names = new Dictionary<string, string>();
            foreach (var resource in raw)
            {
                if (resource == null || string.IsNullOrWhiteSpace(resource.Id) ||
                    string.IsNullOrWhiteSpace(resource.DisplayName) || names.ContainsKey(resource.Id))
                    throw new ArgumentException("Invalid/duplicate resource definitions.");
                names.Add(resource.Id, resource.DisplayName);
            }
            foreach (var recipe in list)
            {
                if (names.ContainsKey(recipe.OutputId)) throw new ArgumentException("Duplicate product definition or raw resource collision.");
                names.Add(recipe.OutputId, recipe.OutputName);
            }
            foreach (var recipe in list)
                if (recipe.Inputs.Keys.Any(id => !names.ContainsKey(id)))
                    throw new ArgumentException("Recipe references unknown ingredient.");
            Recipes = list.AsReadOnly();
            ItemNames = new ReadOnlyDictionary<string, string>(names);
        }

        // Strict unquoted CSV, semicolon-separated item:quantity inputs. '#' lines are comments.
        public static ProductionCatalog Load(string csv, IEnumerable<ResourceDefinition> resources, bool allowDevelopment = false)
        {
            var recipes = new List<ProductionRecipe>();
            foreach (var line in GatheringCatalog.DataRows(csv))
            {
                var row = line.Split(',').Select(x => x.Trim()).ToArray();
                if (row.Length != 13) throw new ArgumentException("Recipe CSV requires 13 fields.");
                if (row[11] != "approved" && row[11] != "development" && row[11] != "new-assumption") throw new ArgumentException("Unknown recipe authority status.");
                if (!Enum.TryParse(row[3], out ProductionCategory category) ||
                    !Enum.IsDefined(typeof(ProductionCategory), category) || category.ToString() != row[3])
                    throw new ArgumentException("Unknown production category.");
                decimal seconds = decimal.Parse(row[9], CultureInfo.InvariantCulture);
                decimal ticks = seconds * TimeSpan.TicksPerSecond;
                if (ticks <= 0 || ticks > long.MaxValue || ticks != decimal.Truncate(ticks))
                    throw new ArgumentException("Duration must be positive integral ticks.");
                var inputs = row[7].Split(';').Select(field =>
                {
                    var pair = field.Split(':');
                    if (pair.Length != 2) throw new ArgumentException("Input syntax is item:quantity.");
                    return new KeyValuePair<string, long>(pair[0], long.Parse(pair[1], CultureInfo.InvariantCulture));
                }).ToArray();
                recipes.Add(new ProductionRecipe(row[0], row[1], row[2], category, row[4], row[5],
                    long.Parse(row[6], CultureInfo.InvariantCulture), inputs,
                    int.Parse(row[8], CultureInfo.InvariantCulture), (long)ticks,
                    row[10] == "TBD" ? (decimal?)null : decimal.Parse(row[10], CultureInfo.InvariantCulture),
                    row[11] == "development", row[12], row[11] == "new-assumption"));
            }
            return new ProductionCatalog(recipes, resources, allowDevelopment);
        }
    }
}
