using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntelOrca.Biohazard.BioRand
{
    public sealed class RandomizerConfigurationDefinition
    {
        public List<Page> Pages { get; set; } = [];

        [JsonIgnore]
        public IEnumerable<GroupItem> AllItems
        {
            get
            {
                foreach (var p in Pages)
                {
                    foreach (var g in p.Groups)
                    {
                        foreach (var i in g.Items)
                        {
                            yield return i;
                        }
                    }
                }
            }
        }

        public Page CreatePage(string label)
        {
            var result = new Page(label);
            Pages.Add(result);
            return result;
        }

        public sealed class Page(string label)
        {
            public string Label { get; set; } = label;
            public List<Group> Groups { get; set; } = [];

            public Group CreateGroup(string label)
            {
                var result = new Group(label);
                Groups.Add(result);
                return result;
            }
        }

        public sealed class Group(string label)
        {
            public string Label { get; set; } = label;
            public string? Warning { get; set; }
            public List<GroupItem> Items { get; set; } = [];
        }

        public sealed class GroupItem
        {
            public string? Id { get; set; }
            public string? Label { get; set; }
            public string? Description { get; set; }
            public GroupItemCategory? Category { get; set; }
            public string? Type { get; set; }
            public int? Size { get; set; }
            public double? Min { get; set; }
            public double? Max { get; set; }
            public double? Step { get; set; }
            public string[]? Options { get; set; }
            public object? Default { get; set; }
        }

        public sealed class GroupItemCategory
        {
            public GroupItemCategory() { }
            public GroupItemCategory(ConfigCategory category)
            {
                Label = category.Label;
                TextColor = category.TextColor;
                BackgroundColor = category.BackgroundColor;
            }

            public string? Label { get; set; }
            public string? TextColor { get; set; }
            public string? BackgroundColor { get; set; }
        }

        public RandomizerConfiguration GetDefault()
        {
            var result = new RandomizerConfiguration();
            foreach (var item in AllItems)
            {
                result[item.Id!] = item.Default!;
            }
            return result;
        }
    }
}
