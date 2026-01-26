using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Shouldly;

namespace Pafiso.Tests;

public class FieldNameResolverTest {
    private class SimpleEntity {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Description { get; set; } = null!;
    }

    private class EntityWithJsonAttributes {
        [JsonPropertyName("user_id")]
        public int Id { get; set; }

        [JsonPropertyName("full_name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("email_address")]
        public string Email { get; set; } = null!;

        public string NoAttribute { get; set; } = null!;
    }

    private class EntityWithNestedProperty {
        public string Name { get; set; } = null!;
        public AddressInfo Address { get; set; } = null!;
    }

    private class AddressInfo {
        [JsonPropertyName("street_name")]
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
    }

    [Test]
    public void ResolvePropertyName_WithoutNamingPolicy_ReturnsSameNameCaseInsensitive() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = null,
            UseJsonPropertyNameAttributes = false
        });

        resolver.ResolvePropertyName<SimpleEntity>("Name").ShouldBe("Name");
        resolver.ResolvePropertyName<SimpleEntity>("name").ShouldBe("Name");
        resolver.ResolvePropertyName<SimpleEntity>("NAME").ShouldBe("Name");
        resolver.ResolvePropertyName<SimpleEntity>("Age").ShouldBe("Age");
        resolver.ResolvePropertyName<SimpleEntity>("age").ShouldBe("Age");
    }

    [Test]
    public void ResolvePropertyName_WithCamelCasePolicy_MapsCorrectly() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UseJsonPropertyNameAttributes = false
        });

        // CamelCase converts "Name" -> "name", so we need to map "name" back to "Name"
        resolver.ResolvePropertyName<SimpleEntity>("name").ShouldBe("Name");
        resolver.ResolvePropertyName<SimpleEntity>("age").ShouldBe("Age");
        resolver.ResolvePropertyName<SimpleEntity>("description").ShouldBe("Description");
    }

    [Test]
    public void ResolvePropertyName_WithSnakeCasePolicy_MapsCorrectly() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UseJsonPropertyNameAttributes = false
        });

        // SnakeCaseLower converts "Description" -> "description", simple properties stay same
        resolver.ResolvePropertyName<SimpleEntity>("name").ShouldBe("Name");
        resolver.ResolvePropertyName<SimpleEntity>("age").ShouldBe("Age");
        resolver.ResolvePropertyName<SimpleEntity>("description").ShouldBe("Description");
    }

    [Test]
    public void ResolvePropertyName_WithJsonPropertyNameAttribute_MapsToPropertyName() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = null,
            UseJsonPropertyNameAttributes = true
        });

        resolver.ResolvePropertyName<EntityWithJsonAttributes>("user_id").ShouldBe("Id");
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("full_name").ShouldBe("Name");
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("email_address").ShouldBe("Email");
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("NoAttribute").ShouldBe("NoAttribute");
    }

    [Test]
    public void ResolvePropertyName_WithJsonPropertyNameAttribute_CaseInsensitive() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = null,
            UseJsonPropertyNameAttributes = true
        });

        resolver.ResolvePropertyName<EntityWithJsonAttributes>("USER_ID").ShouldBe("Id");
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("Full_Name").ShouldBe("Name");
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("EMAIL_ADDRESS").ShouldBe("Email");
    }

    [Test]
    public void ResolvePropertyName_AttributeDisabled_IgnoresJsonPropertyName() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = null,
            UseJsonPropertyNameAttributes = false
        });

        // When attributes are disabled, "user_id" won't match anything directly
        // but direct property names should still work
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("Id").ShouldBe("Id");
        resolver.ResolvePropertyName<EntityWithJsonAttributes>("Name").ShouldBe("Name");
    }

    [Test]
    public void ResolvePropertyName_NestedProperty_ResolvesEachLevel() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UseJsonPropertyNameAttributes = true
        });

        // "address.street_name" should resolve to "Address.Street"
        resolver.ResolvePropertyName<EntityWithNestedProperty>("address.street_name").ShouldBe("Address.Street");
        resolver.ResolvePropertyName<EntityWithNestedProperty>("address.city").ShouldBe("Address.City");
    }

    [Test]
    public void ResolvePropertyName_UnknownProperty_ReturnsSameName() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings());

        // Unknown properties are returned as-is (will fail later during expression building)
        resolver.ResolvePropertyName<SimpleEntity>("unknown_property").ShouldBe("unknown_property");
    }

    [Test]
    public void ResolvePropertyName_EmptyString_ReturnsEmptyString() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings());

        resolver.ResolvePropertyName<SimpleEntity>("").ShouldBe("");
    }

    [Test]
    public void ResolvePropertyName_WithTypeParameter_WorksCorrectly() {
        var resolver = new DefaultFieldNameResolver(new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        });

        resolver.ResolvePropertyName(typeof(EntityWithJsonAttributes), "user_id").ShouldBe("Id");
        resolver.ResolvePropertyName(typeof(EntityWithJsonAttributes), "full_name").ShouldBe("Name");
    }

    [Test]
    public void PassThroughResolver_ReturnsUnchangedFieldName() {
        var resolver = PassThroughFieldNameResolver.Instance;

        resolver.ResolvePropertyName<SimpleEntity>("name").ShouldBe("name");
        resolver.ResolvePropertyName<SimpleEntity>("user_id").ShouldBe("user_id");
        resolver.ResolvePropertyName<SimpleEntity>("anything").ShouldBe("anything");
    }

    [Test]
    public void DefaultFieldNameResolver_UsesDefaultSettingsWhenNull() {
        PafisoSettings.Default = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var resolver = new DefaultFieldNameResolver(null);

        resolver.ResolvePropertyName<SimpleEntity>("name").ShouldBe("Name");
    }

    [Test]
    public void DefaultFieldNameResolver_DefaultConstructor_UsesDefaultSettings() {
        PafisoSettings.Default = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var resolver = new DefaultFieldNameResolver();

        resolver.ResolvePropertyName<SimpleEntity>("name").ShouldBe("Name");
    }

    [TearDown]
    public void TearDown() {
        // Reset default settings after each test
        PafisoSettings.Default = new PafisoSettings();
    }
}
