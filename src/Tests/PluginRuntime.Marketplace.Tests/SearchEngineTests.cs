using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using PluginRuntime.Marketplace.Models;
using PluginRuntime.Marketplace.Search;

namespace PluginRuntime.Marketplace.Tests;

/// <summary>
/// Property tests for the client-side search engine.
/// </summary>
public class SearchEngineTests
{
    private readonly SearchEngine _engine = new();

    [Fact]
    public void EmptyInput_ReturnsEmptyResult()
    {
        var result = _engine.Search([], new SearchCriteria(null, null, null, null));
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void TextSearch_FiltersByName()
    {
        var items = new[]
        {
            MakeExtension("Plugin Alpha", "Tooling", "Low"),
            MakeExtension("Plugin Beta", "Security", "High"),
            MakeExtension("Gamma Tool", "Tooling", "Medium"),
        };

        var result = _engine.Search(items, new SearchCriteria("Alpha", null, null, null));
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Plugin Alpha");
    }

    [Fact]
    public void CategoryFilter_ExactMatch()
    {
        var items = new[]
        {
            MakeExtension("A", "Tooling", "Low"),
            MakeExtension("B", "Security", "High"),
            MakeExtension("C", "Tooling", "Medium"),
        };

        var result = _engine.Search(items, new SearchCriteria(null, "Security", null, null));
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("B");
    }

    [Property(MaxTest = 100)]
    public bool Property2_FilteredResults_AllMatchCriteria(PositiveInt count)
    {
        var n = Math.Min(count.Get, 50);
        var items = Enumerable.Range(0, n)
            .Select(i => MakeExtension($"Ext{i}", i % 2 == 0 ? "Cat1" : "Cat2", "Low"))
            .ToList();

        var result = _engine.Search(items, new SearchCriteria(null, "Cat1", null, null));
        return result.Items.All(i => i.Category == "Cat1");
    }

    [Property(MaxTest = 100)]
    public bool Property3_Pagination_EachPageHasAtMost20Items(PositiveInt count)
    {
        var n = Math.Min(count.Get, 200);
        var items = Enumerable.Range(0, n)
            .Select(i => MakeExtension($"Ext{i}", "Cat", "Low"))
            .ToList();

        var result = _engine.Search(items, new SearchCriteria(null, null, null, null), page: 1, pageSize: 20);
        return result.Items.Count <= 20 && result.TotalCount == n;
    }

    private static ExtensionSummaryDto MakeExtension(string name, string category, string riskLevel) =>
        new(Guid.NewGuid(), name, "Author", category, "1.0.0", riskLevel, $"Description for {name}", 0);
}
