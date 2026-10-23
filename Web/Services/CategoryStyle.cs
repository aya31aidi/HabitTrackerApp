namespace Web.Services
{
    /// <summary>
    /// Gives every category a stable color and a matching icon, based on its name.
    /// </summary>
    public static class CategoryStyle
    {
        private const int PaletteSize = 8;

        private static readonly (string[] Keywords, string Icon)[] IconRules =
        {
            (new[] { "health", "santé", "sante", "medic", "water", "eau" }, "bi-heart-pulse"),
            (new[] { "sport", "fitness", "gym", "run", "exercise", "workout" }, "bi-bicycle"),
            (new[] { "study", "learn", "étude", "etude", "school", "read", "book", "lecture" }, "bi-book"),
            (new[] { "work", "travail", "job", "career", "product" }, "bi-briefcase"),
            (new[] { "mind", "medit", "mental", "calm", "relax" }, "bi-flower1"),
            (new[] { "food", "diet", "nutrition", "eat", "cook" }, "bi-egg-fried"),
            (new[] { "sleep", "sommeil", "rest" }, "bi-moon-stars"),
            (new[] { "finance", "money", "budget", "saving" }, "bi-piggy-bank"),
            (new[] { "social", "family", "friend", "famille" }, "bi-people"),
            (new[] { "hobby", "music", "art", "creative", "loisir" }, "bi-palette")
        };

        public static string Icon(string? name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var lower = name.ToLowerInvariant();
                foreach (var (keywords, icon) in IconRules)
                {
                    if (keywords.Any(lower.Contains))
                        return icon;
                }
            }
            return "bi-tag";
        }

        // Maps to the .tone-0 ... .tone-7 classes in site.css
        public static string Tone(int categoryId) => $"tone-{Math.Abs(categoryId) % PaletteSize}";
    }
}
