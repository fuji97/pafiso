using System;
using System.Text.Json;
using NUnit.Framework;
using Shouldly;

namespace Pafiso.Tests;

public class PafisoSettingsTest {
    [SetUp]
    public void Setup() {
        // Reset to default settings before each test
        PafisoSettings.Default = new PafisoSettings();
    }

    [Test]
    public void DefaultSettings_HaveExpectedValues() {
        var settings = new PafisoSettings();

        settings.PropertyNamingPolicy.ShouldBeNull();
        settings.UseJsonPropertyNameAttributes.ShouldBeTrue();
        settings.StringComparison.ShouldBe(StringComparison.OrdinalIgnoreCase);
        settings.UseEfCoreLikeForCaseInsensitive.ShouldBeTrue();
    }

    [Test]
    public void Clone_CreatesIndependentCopy() {
        var original = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UseJsonPropertyNameAttributes = false,
            StringComparison = StringComparison.InvariantCultureIgnoreCase,
            UseEfCoreLikeForCaseInsensitive = false
        };

        var clone = original.Clone();

        clone.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        clone.UseJsonPropertyNameAttributes.ShouldBeFalse();
        clone.StringComparison.ShouldBe(StringComparison.InvariantCultureIgnoreCase);
        clone.UseEfCoreLikeForCaseInsensitive.ShouldBeFalse();

        // Modify clone and verify original is unchanged
        clone.UseJsonPropertyNameAttributes = true;
        original.UseJsonPropertyNameAttributes.ShouldBeFalse();
    }

    [Test]
    public void StaticDefault_CanBeOverridden() {
        var customSettings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            StringComparison = StringComparison.Ordinal
        };

        PafisoSettings.Default = customSettings;

        PafisoSettings.Default.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.SnakeCaseLower);
        PafisoSettings.Default.StringComparison.ShouldBe(StringComparison.Ordinal);
    }

    [Test]
    public void AllNamingPolicies_AreSupported() {
        // Verify all built-in naming policies can be assigned
        var settings = new PafisoSettings();

        settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        settings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);

        settings.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        settings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.SnakeCaseLower);

        settings.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper;
        settings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.SnakeCaseUpper);

        settings.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
        settings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.KebabCaseLower);

        settings.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseUpper;
        settings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.KebabCaseUpper);
    }

    [Test]
    public void AllStringComparisons_AreSupported() {
        var settings = new PafisoSettings();

        foreach (StringComparison comparison in Enum.GetValues<StringComparison>()) {
            settings.StringComparison = comparison;
            settings.StringComparison.ShouldBe(comparison);
        }
    }
}
