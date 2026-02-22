using FluentAssertions;
using Fonbec.Web.Logic.ExtensionMethods;

namespace Fonbec.Web.Logic.Tests.ExtensionMethods;

public class DetectChangesExtensionMethodsTests
{
    [Fact]
    public void DeepClone_Success()
    {
        var testClass = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var clonedTestClass = testClass.DeepClone();

        clonedTestClass.Id.Should().Be(314);
        clonedTestClass.Name.Should().Be("Test");
        clonedTestClass.SubClass.Should().NotBeNull();
        clonedTestClass.SubClass.Description.Should().Be("SubClass");
        clonedTestClass.SubClass.Numbers.Should().HaveCount(3);
        clonedTestClass.SubClass.Numbers.Should().BeEquivalentTo([3, 1, 4]);
    }

    [Fact]
    public void IsEqualTo_TwoDifferentClasses_BothSameValues_ReturnsTrue()
    {
        var testClass1 = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var testClass2 = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var result = testClass1.IsEqualTo(testClass2);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsEqualTo_TwoDifferentClasses_IdsAreDifferent_ReturnsFalse()
    {
        var testClass1 = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var testClass2 = new TestClass
        {
            Id = 315,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var result = testClass1.IsEqualTo(testClass2);

        result.Should().BeFalse("IsIdenticalTo() compares the Id property");
    }

    [Fact]
    public void IsEqualTo_TwoDifferentClasses_NamesAreDifferent_ReturnsFalse()
    {
        var testClass1 = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var testClass2 = new TestClass
        {
            Id = 314,
            Name = "TestX",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var result = testClass1.IsEqualTo(testClass2);

        result.Should().BeFalse("IsIdenticalTo() compares the Name property");
    }

    [Fact]
    public void IsEqualTo_TwoDifferentClasses_DescriptionssAreDifferent_ReturnsTrue()
    {
        var testClass1 = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };

        var testClass2 = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClassX", Numbers = [3, 1, 4] },
        };

        var result = testClass1.IsEqualTo(testClass2);

        result.Should().BeTrue("IsIdenticalTo() does not compare the Description property");
    }

    [Fact]
    public void IsEqualTo_OtherIsNull_ReturnsFalse()
    {
        var testClass = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };
        var result = testClass.IsEqualTo(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsEqualTo_SameReference_ReturnsTrue()
    {
        var testClass = new TestClass
        {
            Id = 314,
            Name = "Test",
            SubClass = new() { Description = "SubClass", Numbers = [3, 1, 4] },
        };
        var result = testClass.IsEqualTo(testClass);

        result.Should().BeTrue();
    }

    private class TestClass : IDetectChanges<TestClass>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public TestSubClass? SubClass { get; set; }

        // For this test, only the Id and Name properties are going to be considered when testing for equality.
        public bool IsIdenticalTo(TestClass other) =>
            Id == other.Id
            && Name == other.Name;
    }

    private class TestSubClass
    {
        public string Description { get; set; } = string.Empty;
        public List<int> Numbers { get; set; } = new();
    }
}
