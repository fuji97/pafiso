using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Tests.Mapping;

public class FilterWithMapperTests {
    // Test mapping models
    public class UserSearchDto : MappingModel {
        public string? Username { get; set; }
        public int? MinAge { get; set; }
        public string? Email { get; set; }
    }

    // Test entities
    public class User {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Email { get; set; } = null!;
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

}
