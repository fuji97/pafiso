using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Tests.Mapping;

public class FieldMapperTests {
    // Test mapping models
    public class UserSearchDto : MappingModel {
        public string? Username { get; set; }
        public int? MinAge { get; set; }
        public string? Email { get; set; }
    }

    public class ProductSearchDto : MappingModel {
        public string? Name { get; set; }
        public string? MinPrice { get; set; }
        public string? IsActive { get; set; }
    }

    public class OrderSearchDto : MappingModel {
        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("order_date")]
        public DateTime? OrderDate { get; set; }
    }

    // Test entities
    public class User {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Email { get; set; } = null!;
    }

    public class Product {
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
        public bool Active { get; set; }
    }

    public class Order {
        public int Id { get; set; }
        public Customer Customer { get; set; } = null!;
        public DateTime OrderDate { get; set; }
    }

    public class Customer {
        public string Name { get; set; } = null!;
    }

    [Test]
    public void MappingModel_MustInheritFromBaseClass() {
        // Arrange & Act
        var mapper = new FieldMapper<UserSearchDto, User>();

        // Assert - Should compile without errors
        mapper.ShouldNotBeNull();
    }

    [Test]
    public void ResolveToEntityField_Default1To1Mapping_CaseInsensitive() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>();

        // Act
        var emailField = mapper.ResolveToEntityField("email");
        var ageField = mapper.ResolveToEntityField("Age");
        var usernameField = mapper.ResolveToEntityField("USERNAME");

        // Assert
        emailField.ShouldBe("Email");
        ageField.ShouldBe("Age");
        // Username doesn't exist on User entity (it's "Name"), so should return null
        usernameField.ShouldBeNull();
    }

    [Test]
    public void ResolveToEntityField_CustomFieldMapping() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name)
            .Map(dto => dto.MinAge, entity => entity.Age);

        // Act
        var usernameField = mapper.ResolveToEntityField("username");
        var minAgeField = mapper.ResolveToEntityField("minAge");

        // Assert
        usernameField.ShouldBe("Name");
        minAgeField.ShouldBe("Age");
    }

    [Test]
    public void ResolveToEntityField_InvalidField_ReturnsNull() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>();

        // Act
        var invalidField = mapper.ResolveToEntityField("nonExistentField");
        var nullField = mapper.ResolveToEntityField(null!);
        var emptyField = mapper.ResolveToEntityField("");

        // Assert
        invalidField.ShouldBeNull();
        nullField.ShouldBeNull();
        emptyField.ShouldBeNull();
    }

    [Test]
    public void ResolveToEntityField_NestedProperty_Works() {
        // Arrange
        var mapper = new FieldMapper<OrderSearchDto, Order>()
            .Map(dto => dto.CustomerName, entity => entity.Customer.Name);

        // Act
        var customerNameField = mapper.ResolveToEntityField("customer_name");

        // Assert
        customerNameField.ShouldBe("Customer.Name");
    }

    [Test]
    public void TransformValue_NoTransformer_ReturnsRawValue() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>();

        // Act
        var value = mapper.TransformValue<string>("username", "john");

        // Assert
        value.ShouldBe("john");
    }

    [Test]
    public void TransformValue_WithCustomTransformer_TransformsValue() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.Name, e => e.ProductName)
            .MapWithTransform(dto => dto.MinPrice, e => e.Price, val => {
                if (decimal.TryParse(val, out var price)) {
                    return price;
                }
                return 0m;
            })
            .MapWithTransform(dto => dto.IsActive, e => e.Active, val => {
                return bool.Parse(val ?? "false");
            });

        // Act
        var priceValue = mapper.TransformValue<decimal>("minPrice", "50");
        var boolValue = mapper.TransformValue<bool>("isActive", "true");

        // Assert
        priceValue.ShouldBe(50m);
        boolValue.ShouldBe(true);
    }

    [Test]
    public void TransformValue_TransformerFails_ReturnsNull() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .MapWithTransform(dto => dto.MinPrice, e => e.Price, val => {
                return decimal.Parse(val!); // Will throw on invalid input
            });

        // Act
        var result = mapper.TransformValue<decimal>("minPrice", "invalid");

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void GetMappedFields_ReturnsAllValidFields() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        // Act
        var fields = mapper.GetMappedFields();

        // Assert
        fields.ShouldContain("Username");
        fields.ShouldContain("MinAge");
        fields.ShouldContain("Email");
        fields.Count.ShouldBe(3);
    }

    [Test]
    public void Map_WithStringNames_Works() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>();

        // Act
        mapper.Map("username", "Name");
        var resolvedField = mapper.ResolveToEntityField("username");

        // Assert
        resolvedField.ShouldBe("Name");
    }

    [Test]
    public void Map_InvalidEntityField_ThrowsException() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => mapper.Map("username", "NonExistentField"));
    }

    [Test]
    public void JsonNamingPolicy_IntegrationWithResolver() {
        // Arrange
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var mapper = new FieldMapper<OrderSearchDto, Order>(settings)
            .Map(dto => dto.CustomerName, entity => entity.Customer.Name);

        // Act - incoming field name uses JsonPropertyName attribute "customer_name"
        var resolvedField = mapper.ResolveToEntityField("customer_name");

        // Assert
        resolvedField.ShouldBe("Customer.Name");
    }

    [Test]
    public void WithTransform_WithoutMapping_AppliesTransformOnly() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .WithTransform<int>(dto => dto.MinAge, val => {
                if (int.TryParse(val, out var age)) {
                    return age * 2; // Double the age for testing
                }
                return 0;
            });

        // Act
        var transformed = mapper.TransformValue<int>("minAge", "25");

        // Assert
        transformed.ShouldBe(50);
    }

    [Test]
    public void MappingModel_OnBeforeMap_CanBeOverridden() {
        // Arrange
        var model = new TestMappingModel();

        // Act
        var result = model.OnBeforeMap();

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void MappingModel_Validate_CanBeOverridden() {
        // Arrange
        var model = new TestMappingModel();

        // Act
        var result = model.Validate();

        // Assert
        result.ShouldBeTrue();
    }

    private class TestMappingModel : MappingModel {
        public override bool OnBeforeMap() => true;
        public override bool Validate() => true;
    }
}
