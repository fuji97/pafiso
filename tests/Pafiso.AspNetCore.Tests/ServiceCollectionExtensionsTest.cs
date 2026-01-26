using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pafiso.AspNetCore;
using Shouldly;

namespace Pafiso.AspNetCore.Tests;

public class ServiceCollectionExtensionsTest {
    [SetUp]
    public void Setup() {
        // Reset default settings before each test
        PafisoSettings.Default = new PafisoSettings();
    }

    [TearDown]
    public void TearDown() {
        PafisoSettings.Default = new PafisoSettings();
    }

    [Test]
    public void AddPafiso_RegistersSettings() {
        var services = new ServiceCollection();

        services.AddPafiso();

        var provider = services.BuildServiceProvider();
        var settings = provider.GetService<PafisoSettings>();

        settings.ShouldNotBeNull();
    }

    [Test]
    public void AddPafiso_WithConfigure_AppliesConfiguration() {
        var services = new ServiceCollection();

        services.AddPafiso(settings => {
            settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            settings.StringComparison = StringComparison.InvariantCultureIgnoreCase;
            settings.UseEfCoreLikeForCaseInsensitive = false;
        });

        var provider = services.BuildServiceProvider();
        var settings = provider.GetService<PafisoSettings>();

        settings.ShouldNotBeNull();
        settings!.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        settings.StringComparison.ShouldBe(StringComparison.InvariantCultureIgnoreCase);
        settings.UseEfCoreLikeForCaseInsensitive.ShouldBeFalse();
    }

    [Test]
    public void AddPafiso_SetsGlobalDefault() {
        var services = new ServiceCollection();

        services.AddPafiso(settings => {
            settings.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

        var provider = services.BuildServiceProvider();
        _ = provider.GetService<PafisoSettings>();

        PafisoSettings.Default.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.SnakeCaseLower);
    }

    [Test]
    public void AddPafiso_WithPreConfiguredSettings_RegistersInstance() {
        var services = new ServiceCollection();
        var preConfigured = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
            UseJsonPropertyNameAttributes = false
        };

        services.AddPafiso(preConfigured);

        var provider = services.BuildServiceProvider();
        var settings = provider.GetService<PafisoSettings>();

        settings.ShouldNotBeNull();
        settings.ShouldBeSameAs(preConfigured);
        settings!.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.KebabCaseLower);
        settings.UseJsonPropertyNameAttributes.ShouldBeFalse();
    }

    [Test]
    public void AddPafiso_WithPreConfiguredSettings_SetsGlobalDefault() {
        var services = new ServiceCollection();
        var preConfigured = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        services.AddPafiso(preConfigured);

        PafisoSettings.Default.ShouldBeSameAs(preConfigured);
    }

    [Test]
    public void AddPafiso_AutoDetectsFromMvcJsonOptions() {
        var services = new ServiceCollection();

        // Configure MVC JsonOptions
        services.Configure<JsonOptions>(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddPafiso();

        var provider = services.BuildServiceProvider();
        var settings = provider.GetService<PafisoSettings>();

        settings.ShouldNotBeNull();
        settings!.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
    }

    [Test]
    public void AddPafiso_CustomConfigurationOverridesAutoDetect() {
        var services = new ServiceCollection();

        // Configure MVC JsonOptions with CamelCase
        services.Configure<JsonOptions>(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        // Override with SnakeCaseLower
        services.AddPafiso(settings => {
            settings.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

        var provider = services.BuildServiceProvider();
        var settings = provider.GetService<PafisoSettings>();

        settings.ShouldNotBeNull();
        settings!.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.SnakeCaseLower);
    }

    [Test]
    public void AddPafiso_WithoutMvcJsonOptions_UsesDefaults() {
        var services = new ServiceCollection();

        services.AddPafiso();

        var provider = services.BuildServiceProvider();
        var settings = provider.GetService<PafisoSettings>();

        settings.ShouldNotBeNull();
        settings!.PropertyNamingPolicy.ShouldBeNull();
        settings.UseJsonPropertyNameAttributes.ShouldBeTrue();
        settings.StringComparison.ShouldBe(StringComparison.OrdinalIgnoreCase);
    }
}

public class QueryCollectionExtensionsWithSettingsTest {
    [SetUp]
    public void Setup() {
        PafisoSettings.Default = new PafisoSettings();
    }

    [TearDown]
    public void TearDown() {
        PafisoSettings.Default = new PafisoSettings();
    }

    [Test]
    public void ToSearchParameters_WithSettings_PreservesFieldNames() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "user_name" },
            { "filters[0][op]", "contains" },
            { "filters[0][val]", "john" }
        });

        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var searchParameters = query.ToSearchParameters(settings);

        // Field names should be preserved as-is; resolution happens during ApplyToIQueryable
        searchParameters.Filters.Count.ShouldBe(1);
        searchParameters.Filters[0].Fields.ShouldBe(["user_name"]);
    }

    [Test]
    public void ToSearchParameters_WithSettings_UsesRegisteredSettings() {
        var services = new ServiceCollection();
        services.AddPafiso(settings => {
            settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var provider = services.BuildServiceProvider();
        var pafisoSettings = provider.GetRequiredService<PafisoSettings>();

        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "userName" },
            { "filters[0][op]", "eq" },
            { "filters[0][val]", "test" }
        });

        var searchParameters = query.ToSearchParameters(pafisoSettings);

        searchParameters.ShouldNotBeNull();
        searchParameters.Filters.Count.ShouldBe(1);
    }

    [Test]
    public void ToSearchParameters_WithSettings_AutoDetectsFromMvcJsonOptions() {
        var services = new ServiceCollection();

        // Configure MVC options and AddPafiso
        services.Configure<JsonOptions>(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        services.AddPafiso();

        var provider = services.BuildServiceProvider();
        var pafisoSettings = provider.GetRequiredService<PafisoSettings>();

        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "sortings[0][prop]", "createdAt" },
            { "sortings[0][ord]", "desc" }
        });

        // Should work using auto-detected settings
        var searchParameters = query.ToSearchParameters(pafisoSettings);

        searchParameters.ShouldNotBeNull();
        searchParameters.Sortings.Count.ShouldBe(1);
        searchParameters.Sortings[0].PropertyName.ShouldBe("createdAt");
    }

    [Test]
    public void ToSearchParameters_WithoutDI_FallsBackToDefault() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "Name" },
            { "filters[0][op]", "eq" },
            { "filters[0][val]", "Test" }
        });

        var searchParameters = query.ToSearchParameters();

        searchParameters.ShouldNotBeNull();
        searchParameters.Filters.Count.ShouldBe(1);
    }

    [Test]
    public void ToSearchParameters_WithNullSettings_UsesDefaults() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "Name" },
            { "filters[0][op]", "eq" },
            { "filters[0][val]", "Test" }
        });

        var searchParameters = query.ToSearchParameters((PafisoSettings?)null);

        searchParameters.ShouldNotBeNull();
        searchParameters.Filters.Count.ShouldBe(1);
    }
}
