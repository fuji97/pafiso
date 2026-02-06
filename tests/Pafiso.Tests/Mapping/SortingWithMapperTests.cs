using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Tests.Mapping;

public class SortingWithMapperTests {
    // Test mapping models
    public class UserSearchDto : MappingModel {
        public string? UserName { get; set; }
        public int? UserAge { get; set; }
    }

    // Test entities
    public class User {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    [Test]
    public void ApplyToIQueryable_WithMapper_ResolvesFieldCorrectly() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.UserName, entity => entity.Name)
            .Map(dto => dto.UserAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "Charlie", Age = 30 },
            new() { Id = 2, Name = "Alice", Age = 25 },
            new() { Id = 3, Name = "Bob", Age = 35 }
        }.AsQueryable();

        // Create sorting with mapper
        var sorting = Sorting.WithMapper("userName", SortOrder.Ascending, mapper);

        // Act
        var result = sorting.ApplyToIQueryable(users).ToList();

        // Assert
        result[0].Name.ShouldBe("Alice");
        result[1].Name.ShouldBe("Bob");
        result[2].Name.ShouldBe("Charlie");
    }

    [Test]
    public void ApplyToIQueryable_WithMapper_Descending() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.UserAge, entity => entity.Age);

        var users = new List<User> {
            new() { Id = 1, Name = "Charlie", Age = 30 },
            new() { Id = 2, Name = "Alice", Age = 25 },
            new() { Id = 3, Name = "Bob", Age = 35 }
        }.AsQueryable();

        var sorting = Sorting.WithMapper("userAge", SortOrder.Descending, mapper);

        // Act
        var result = sorting.ApplyToIQueryable(users).ToList();

        // Assert
        result[0].Age.ShouldBe(35);
        result[1].Age.ShouldBe(30);
        result[2].Age.ShouldBe(25);
    }

    [Test]
    public void ThenApplyToIQueryable_WithMapper_MultipleSorts() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.UserAge, entity => entity.Age)
            .Map(dto => dto.UserName, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "Charlie", Age = 30 },
            new() { Id = 2, Name = "Alice", Age = 30 },
            new() { Id = 3, Name = "Bob", Age = 25 }
        }.AsQueryable();

        var primarySort = Sorting.WithMapper("userAge", SortOrder.Ascending, mapper);
        var secondarySort = Sorting.WithMapper("userName", SortOrder.Ascending, mapper);

        // Act
        var orderedQuery = primarySort.ApplyToIQueryable(users);
        var result = secondarySort.ThenApplyToIQueryable(orderedQuery).ToList();

        // Assert - First by age, then by name
        result[0].Name.ShouldBe("Bob");    // Age 25
        result[1].Name.ShouldBe("Alice");  // Age 30, name A
        result[2].Name.ShouldBe("Charlie"); // Age 30, name C
    }

    [Test]
    public void ApplyToIQueryable_WithMapper_InvalidField_ThrowsException() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.UserName, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "Charlie", Age = 30 }
        }.AsQueryable();

        var sorting = Sorting.WithMapper("invalidField", SortOrder.Ascending, mapper);

        // Act & Assert - Invalid field should throw
        Should.Throw<InvalidOperationException>(() => sorting.ApplyToIQueryable(users));
    }

    [Test]
    public void ThenApplyToIQueryable_WithMapper_InvalidField_ReturnsQueryUnchanged() {
        // Arrange
        var mapper = new FieldMapper<UserSearchDto, User>()
            .Map(dto => dto.UserName, entity => entity.Name);

        var users = new List<User> {
            new() { Id = 1, Name = "Charlie", Age = 30 },
            new() { Id = 2, Name = "Alice", Age = 25 }
        }.AsQueryable();

        var primarySort = Sorting.WithMapper("userName", SortOrder.Ascending, mapper);
        var invalidSort = Sorting.WithMapper("invalidField", SortOrder.Ascending, mapper);

        // Act
        var orderedQuery = primarySort.ApplyToIQueryable(users);
        var result = invalidSort.ThenApplyToIQueryable(orderedQuery).ToList();

        // Assert - Should maintain primary sort only
        result[0].Name.ShouldBe("Alice");
        result[1].Name.ShouldBe("Charlie");
    }
}
