using System;
using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;

using Pafiso.Util;
namespace Pafiso.Tests; 

public class ExpressionTests {
    private class Bar {
        public int Text { get; set; }
    }

    private class Foo {
        public Bar Bar { get; set; } = null!;
    }

    private class EnumWrapper {
        public TestEnum TestEnum { get; set; }
    }

    private enum TestEnum {
        Test
    }

    [Test]
    public void ExpressionDecomposerPathNavigation() {
        Expression<Func<Foo, object>> expr = foo => foo.Bar.Text;

        var path = ExpressionUtilities.ExpressionDecomposer(expr.Body);
        
        path.Should().Be($"{nameof(Foo.Bar)}.{nameof(Bar.Text)}");
    }

    [Test]
    public void ExpressionDecomposerCasting() {
        Expression<Func<Bar, float>> expr = bar => bar.Text;

        var path = ExpressionUtilities.ExpressionDecomposer(expr.Body);
        
        path.Should().Be($"{nameof(Bar.Text)}");
    }
    
    [Test]
    public void ExpressionDecomposerEnumToInt() {
        Expression<Func<EnumWrapper, int>> expr = e => (int) e.TestEnum;

        var path = ExpressionUtilities.ExpressionDecomposer(expr.Body);
        
        path.Should().Be($"{nameof(EnumWrapper.TestEnum)}");
    }

    [Test]
    public void GetPropertyValueNestedObject() {
        var foobar = new Foo() { Bar = new Bar() { Text = 10 } };
        
        var value = ExpressionUtilities.GetPropertyValue(foobar, $"{nameof(Foo.Bar)}.{nameof(Bar.Text)}");
        
        value.Should().Be(10);
    }
    
    [Test]
    public void GetPropertyValueWithEnum() {
        var foobar = new EnumWrapper() { TestEnum = TestEnum.Test };
        
        var value = ExpressionUtilities.GetPropertyValue(foobar, nameof(EnumWrapper.TestEnum));
        
        value.Should().Be(TestEnum.Test);
    }
}
