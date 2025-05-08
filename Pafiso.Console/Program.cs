using System.Text.Json;
using Pafiso;
using Pafiso.Util;

var tests = new List<Foo>() {
    new Foo(Test.One),
    new Foo(Test.Two),
    new Foo(Test.Zero),
};

var filter = Filter.FromExpression<Foo>(x => x.Test == Test.One);

var result = tests.Where(filter);

Console.WriteLine(JsonSerializer.Serialize(result));

public enum Test {
    Zero,
    One,
    Two,
}

public class Foo {
    public Test Test { get; set; }

    public Foo(Test test) {
        Test = test;
    }
}