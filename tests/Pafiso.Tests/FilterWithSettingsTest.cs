using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Pafiso.Extensions;
using Shouldly;

namespace Pafiso.Tests;

public class FilterWithSettingsTest {
    private class User {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Email { get; set; } = null!;
    }

    private class UserWithJsonAttributes {
        [JsonPropertyName("user_name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("user_age")]
        public int Age { get; set; }

        [JsonPropertyName("email_address")]
        public string Email { get; set; } = null!;
    }

    private List<User> _users = null!;
    private List<UserWithJsonAttributes> _usersWithAttributes = null!;

    [SetUp]
    public void Setup() {
        _users = [
            new User { Name = "John", Age = 30, Email = "john@example.com" },
            new User { Name = "Jane", Age = 25, Email = "jane@example.com" },
            new User { Name = "JOHN", Age = 35, Email = "JOHN@EXAMPLE.COM" },
            new User { Name = "alice", Age = 28, Email = "alice@example.com" }
        ];

        _usersWithAttributes = [
            new UserWithJsonAttributes { Name = "John", Age = 30, Email = "john@example.com" },
            new UserWithJsonAttributes { Name = "Jane", Age = 25, Email = "jane@example.com" },
            new UserWithJsonAttributes { Name = "Joe", Age = 35, Email = "joe@example.com" }
        ];

        // Reset default settings
        PafisoSettings.Default = new PafisoSettings();
    }

    [TearDown]
    public void TearDown() {
        PafisoSettings.Default = new PafisoSettings();
    }

    #region Field Name Resolution Tests

    [Test]
    public void ApplyFilter_WithCamelCaseNamingPolicy_ResolvesFieldNames() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Use camelCase field name
        var filter = new Filter("name", FilterOperator.Equals, "john");

        var filtered = filter.ApplyFilter(_users.AsQueryable(), settings).ToList();

        // Should match "John" and "JOHN" (case-insensitive by default)
        filtered.Count.ShouldBe(2);
    }

    [Test]
    public void ApplyFilter_WithJsonPropertyNameAttribute_ResolvesFieldNames() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        // Use the JSON property name
        var filter = new Filter("user_name", FilterOperator.Equals, "john");

        var filtered = filter.ApplyFilter(_usersWithAttributes.AsQueryable(), settings).ToList();

        filtered.Count.ShouldBe(1);
        filtered[0].Name.ShouldBe("John");
    }

    [Test]
    public void ApplyFilter_WithJsonPropertyName_AgeFilter() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var filter = new Filter("user_age", FilterOperator.GreaterThan, "25");

        var filtered = filter.ApplyFilter(_usersWithAttributes.AsQueryable(), settings).ToList();

        filtered.Count.ShouldBe(2);
        filtered.ShouldAllBe(u => u.Age > 25);
    }

    #endregion

    #region StringComparison Tests

    [Test]
    public void ApplyFilter_WithStringComparisonOrdinalIgnoreCase_MatchesCaseInsensitive() {
        var settings = new PafisoSettings {
            StringComparison = StringComparison.OrdinalIgnoreCase,
            UseEfCoreLikeForCaseInsensitive = false
        };

        var filter = new Filter("Name", FilterOperator.Equals, "john", caseSensitive: false);

        var filtered = filter.ApplyFilter(_users.AsQueryable(), settings).ToList();

        // Should match "John", "JOHN"
        filtered.Count.ShouldBe(2);
        filtered.ShouldContain(u => u.Name == "John");
        filtered.ShouldContain(u => u.Name == "JOHN");
    }

    [Test]
    public void ApplyFilter_WithStringComparisonOrdinal_CaseSensitive() {
        var settings = new PafisoSettings {
            StringComparison = StringComparison.Ordinal,
            UseEfCoreLikeForCaseInsensitive = false
        };

        // Case-sensitive filter
        var filter = new Filter("Name", FilterOperator.Equals, "John", caseSensitive: true);

        var filtered = filter.ApplyFilter(_users.AsQueryable(), settings).ToList();

        // Should only match exact "John"
        filtered.Count.ShouldBe(1);
        filtered[0].Name.ShouldBe("John");
    }

    [Test]
    public void ApplyFilter_ContainsWithStringComparison_WorksCorrectly() {
        var settings = new PafisoSettings {
            StringComparison = StringComparison.OrdinalIgnoreCase,
            UseEfCoreLikeForCaseInsensitive = false
        };

        var filter = new Filter("Name", FilterOperator.Contains, "OHN", caseSensitive: false);

        var filtered = filter.ApplyFilter(_users.AsQueryable(), settings).ToList();

        // Should match "John" and "JOHN"
        filtered.Count.ShouldBe(2);
    }

    [Test]
    public void ApplyFilter_NotContainsWithStringComparison_WorksCorrectly() {
        var settings = new PafisoSettings {
            StringComparison = StringComparison.OrdinalIgnoreCase,
            UseEfCoreLikeForCaseInsensitive = false
        };

        var filter = new Filter("Name", FilterOperator.NotContains, "john", caseSensitive: false);

        var filtered = filter.ApplyFilter(_users.AsQueryable(), settings).ToList();

        // Should match "Jane" and "alice"
        filtered.Count.ShouldBe(2);
        filtered.ShouldNotContain(u => u.Name.Equals("john", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Extension Methods with Settings Tests

    [Test]
    public void WhereExtension_WithSettings_AppliesFieldNameResolution() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var filter = new Filter("user_name", FilterOperator.Equals, "Jane");

        var filtered = _usersWithAttributes.AsQueryable().Where(filter, settings).ToList();

        filtered.Count.ShouldBe(1);
        filtered[0].Name.ShouldBe("Jane");
    }

    [Test]
    public void WhereExtension_WithRestrictionsAndSettings_WorksTogether() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var restrictions = new FieldRestrictions()
            .AllowFiltering("Name", "Age");

        var filter = new Filter("user_name", FilterOperator.Equals, "John");

        var filtered = _usersWithAttributes.AsQueryable().Where(filter, restrictions, settings).ToList();

        filtered.Count.ShouldBe(1);
    }

    [Test]
    public void WhereExtension_OnIEnumerable_WithSettings() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var filter = new Filter("user_age", FilterOperator.LessThan, "30");

        var filtered = _usersWithAttributes.Where(filter, settings).ToList();

        filtered.Count.ShouldBe(1);
        filtered[0].Name.ShouldBe("Jane");
    }

    #endregion

    #region Combined Restrictions and Settings Tests

    [Test]
    public void ApplyFilter_WithRestrictionsAndSettings_RespectsRestrictions() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        // Only allow Name field (after resolution)
        var restrictions = new FieldRestrictions()
            .AllowFiltering("Name");

        // Filter on email_address should be blocked after resolution to "Email"
        var filter = new Filter("email_address", FilterOperator.Contains, "john");

        var originalCount = _usersWithAttributes.Count;
        var filtered = filter.ApplyFilter(_usersWithAttributes.AsQueryable(), restrictions, settings).ToList();

        // Since Email is not allowed, filter should not be applied
        filtered.Count.ShouldBe(originalCount);
    }

    #endregion
}
