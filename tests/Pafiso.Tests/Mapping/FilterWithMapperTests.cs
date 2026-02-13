using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Pafiso.Enums;
using Pafiso.Expressions;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Tests.Mapping;

public class FilterWithMapperTests {
    // Test mapping models
    public class UserSearchDto : MappingModel {
        public string? Username { get; set; }
        public int? MinAge { get; set; }
        public string? Email { get; set; }
        public string? Status { get; set; }
    }

    // Test entities
    public class User {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Email { get; set; } = null!;
        public string? Status { get; set; }
    }

    [Test]
    public void ApplyFilter_WithMapper_ResolvesFieldsCorrectly() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name)
            .Map(dto => dto.MinAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        // Create filter with mapper
        var filter = Filter.WithMapper("username", FilterOperator.Contains, "John", mapper);

        // Act
        var result = filter.ApplyFilter(users).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("John");
    }

    [Test]
    public void ApplyFilter_WithMapper_MultipleFields() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        // Create filter with multiple fields (OR logic)
        var filter = Filter.WithMapper(new[] { "username", "email" }, FilterOperator.Contains, "john", mapper, false);

        // Act
        var result = filter.ApplyFilter(users).ToList();

        // Assert - Should match both Name and Email fields
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("John");
    }

    [Test]
    public void ApplyFilter_WithMapper_InvalidField_Ignored() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" }
        }.AsQueryable();

        // Create filter with invalid field
        var filter = Filter.WithMapper("invalidField", FilterOperator.Equals, "test", mapper);

        // Act
        var result = filter.ApplyFilter(users).ToList();

        // Assert - Invalid field should be ignored, returns all
        result.Count.ShouldBe(2);
    }

    [Test]
    public void ApplyFilter_WithMapper_NumericComparison() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.MinAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        // Create filter for age >= 30
        var filter = Filter.WithMapper("minAge", FilterOperator.GreaterThanOrEquals, "30", mapper);

        // Act
        var result = filter.ApplyFilter(users).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(u => u.Name == "John");
        result.ShouldContain(u => u.Name == "Bob");
    }

    [Test]
    public void ApplyFilter_WithCustomExpressionBuilder_UsesCustomBuilder() {
        // Arrange
        var customBuilder = new TrackingFilterExpressionBuilder();
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("username", FilterOperator.Equals, "john", mapper, customBuilder, false);

        // Act
        var result = filter.ApplyFilter(users).ToList();

        // Assert - custom builder was invoked
        customBuilder.CallCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public void ApplyFilter_NotEquals_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("username", FilterOperator.NotEquals, "john", mapper, false);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Jane");
    }

    [Test]
    public void ApplyFilter_NotContains_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("username", FilterOperator.NotContains, "j", mapper, false);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Bob");
    }

    [Test]
    public void ApplyFilter_LessThan_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.MinAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("minAge", FilterOperator.LessThan, "30", mapper);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Jane");
    }

    [Test]
    public void ApplyFilter_LessThanOrEquals_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.MinAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("minAge", FilterOperator.LessThanOrEquals, "30", mapper);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(2);
        result.ShouldContain(u => u.Name == "John");
        result.ShouldContain(u => u.Name == "Jane");
    }

    [Test]
    public void ApplyFilter_GreaterThan_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.MinAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("minAge", FilterOperator.GreaterThan, "30", mapper);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Bob");
    }

    [Test]
    public void ApplyFilter_Null_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Status, entity => entity.Status);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com", Status = "active" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com", Status = null },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com", Status = null }
        }.AsQueryable();

        var filter = Filter.WithMapper("status", FilterOperator.Null, null, mapper);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(u => u.Status == null);
    }

    [Test]
    public void ApplyFilter_NotNull_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Status, entity => entity.Status);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com", Status = "active" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com", Status = null },
            new() { Id = 3, Name = "Bob", Age = 35, Email = "bob@test.com", Status = "inactive" }
        }.AsQueryable();

        var filter = Filter.WithMapper("status", FilterOperator.NotNull, null, mapper);

        var result = filter.ApplyFilter(users).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(u => u.Status != null);
    }

    [Test]
    public void ApplyFilter_Equals_CaseInsensitive_FiltersCorrectly() {
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.Username, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "John", Age = 30, Email = "john@test.com" },
            new() { Id = 2, Name = "Jane", Age = 25, Email = "jane@test.com" }
        }.AsQueryable();

        var filter = Filter.WithMapper("username", FilterOperator.Equals, "john", mapper, false);

        var result = filter.ApplyFilter(users, new PafisoSettings {
            StringComparison = StringComparison.OrdinalIgnoreCase
        }).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("John");
    }

    private class TrackingFilterExpressionBuilder : IFilterExpressionBuilder {
        public int CallCount { get; private set; }

        public Expression<Func<T, bool>> BuildFilterExpression<T>(
            string propName, string paramName, FilterOperator op,
            string? value, bool caseSensitive, PafisoSettings settings) {
            CallCount++;
            return DefaultFilterExpressionBuilder.Instance.BuildFilterExpression<T>(
                propName, paramName, op, value, caseSensitive, settings);
        }
    }
}
