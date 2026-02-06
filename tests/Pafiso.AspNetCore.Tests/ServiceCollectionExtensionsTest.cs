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

    // Tests for ToSearchParameters with settings have been removed as the API now requires a mapper
    // Settings are passed to ApplyToIQueryable instead
}
