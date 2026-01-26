using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Pafiso.Util;
using Shouldly;

namespace Pafiso.EntityFrameworkCore.Tests;

public class EfCoreExpressionBuilderTest {
    [SetUp]
    public void Setup() {
        // Reset before each test to ensure clean state
        ExpressionUtilities.EfCoreLikeExpressionBuilder = null;
    }

    [TearDown]
    public void TearDown() {
        // Reset the delegate after tests
        ExpressionUtilities.EfCoreLikeExpressionBuilder = null;
    }

    [Test]
    public void Register_SetsEfCoreLikeExpressionBuilder() {
        ExpressionUtilities.EfCoreLikeExpressionBuilder.ShouldBeNull();

        EfCoreExpressionBuilder.Register();

        ExpressionUtilities.EfCoreLikeExpressionBuilder.ShouldNotBeNull();
    }

    [Test]
    public void Register_CalledMultipleTimes_DoesNotThrow() {
        Should.NotThrow(() => {
            EfCoreExpressionBuilder.Register();
            EfCoreExpressionBuilder.Register();
            EfCoreExpressionBuilder.Register();
        });

        ExpressionUtilities.EfCoreLikeExpressionBuilder.ShouldNotBeNull();
    }

    [Test]
    public void BuildLikeExpression_ReturnsMethodCallExpression() {
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var memberExpression = Expression.Property(param, nameof(TestEntity.Name));

        var result = EfCoreExpressionBuilder.BuildLikeExpression(memberExpression, "%test%");

        result.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result;
        methodCall.Method.Name.ShouldBe("Like");
    }

    [Test]
    public void BuildLikeExpression_WithPattern_IncludesPatternInExpression() {
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var memberExpression = Expression.Property(param, nameof(TestEntity.Name));
        var pattern = "%search%";

        var result = EfCoreExpressionBuilder.BuildLikeExpression(memberExpression, pattern);

        var methodCall = (MethodCallExpression)result;
        // The pattern should be the third argument (after DbFunctions and the property)
        methodCall.Arguments.Count.ShouldBe(3);
        var patternArg = (ConstantExpression)methodCall.Arguments[2];
        patternArg.Value.ShouldBe(pattern);
    }

    [Test]
    public void BuildLikeExpression_WithStringProperty_DoesNotConvert() {
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var memberExpression = Expression.Property(param, nameof(TestEntity.Name));

        var result = EfCoreExpressionBuilder.BuildLikeExpression(memberExpression, "%test%");

        result.ShouldNotBeNull();
        var methodCall = (MethodCallExpression)result;
        // Second argument should be the member expression (the Name property)
        methodCall.Arguments[1].ShouldBeAssignableTo<MemberExpression>();
    }

    [Test]
    public void BuildLikeExpression_CanBeCompiledToLambda() {
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var memberExpression = Expression.Property(param, nameof(TestEntity.Name));

        var likeExpression = EfCoreExpressionBuilder.BuildLikeExpression(memberExpression, "%test%");

        // Should be able to create a lambda from the expression
        var lambda = Expression.Lambda<Func<TestEntity, bool>>(likeExpression, param);
        lambda.ShouldNotBeNull();
        lambda.ReturnType.ShouldBe(typeof(bool));
    }

    [Test]
    public void EfCoreLikeExpressionBuilder_Delegate_IsSetAfterRegister() {
        EfCoreExpressionBuilder.Register();

        ExpressionUtilities.EfCoreLikeExpressionBuilder.ShouldNotBeNull();

        var param = Expression.Parameter(typeof(TestEntity), "x");
        var memberExpression = Expression.Property(param, nameof(TestEntity.Name));

        var result = ExpressionUtilities.EfCoreLikeExpressionBuilder!(memberExpression, "%test%");

        result.ShouldBeAssignableTo<MethodCallExpression>();
    }

    [Test]
    public void BuildLikeExpression_ProducesValidEfFunctionsLikeCall() {
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var memberExpression = Expression.Property(param, nameof(TestEntity.Name));

        var result = EfCoreExpressionBuilder.BuildLikeExpression(memberExpression, "%test%");

        var methodCall = (MethodCallExpression)result;
        
        // Verify the method is from DbFunctionsExtensions
        methodCall.Method.DeclaringType.ShouldBe(typeof(DbFunctionsExtensions));
        methodCall.Method.Name.ShouldBe("Like");
        
        // Verify first argument is EF.Functions
        methodCall.Arguments[0].ShouldBeAssignableTo<MemberExpression>();
    }

    private class TestEntity {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
