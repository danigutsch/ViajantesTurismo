using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

#pragma warning disable CA1508 // Avoid dead code (intentional for test coverage)

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_Returns_True_For_Same_Instance()
    {
        var address = new TestAddress("123 Main St", "Springfield");

        var result = address.Equals(address);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Returns_True_For_Equal_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        var result = address1.Equals(address2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Returns_False_For_Different_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("456 Oak Ave", "Springfield");

        var result = address1.Equals(address2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_False_When_Other_Is_Null()
    {
        var address = new TestAddress("123 Main St", "Springfield");

        var result = address.Equals(null);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_False_For_Different_Types()
    {
        var address = new TestAddress("123 Main St", "Springfield");
        var money = new TestMoney(100m, "USD");

        var result = address.Equals(money);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Object_Returns_True_For_Equal_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        object address2 = new TestAddress("123 Main St", "Springfield");

        var result = address1.Equals(address2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Object_Returns_False_For_Non_Value_Object()
    {
        var address = new TestAddress("123 Main St", "Springfield");
        object other = "Not a value object";

        var result = address.Equals(other);

        Assert.False(result);
    }

    [Fact]
    public void Get_Hash_Code_Returns_Same_Value_For_Equal_Objects()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        var hash1 = address1.GetHashCode();
        var hash2 = address2.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Get_Hash_Code_Returns_Different_Values_For_Different_Objects()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("456 Oak Ave", "Springfield");

        var hash1 = address1.GetHashCode();
        var hash2 = address2.GetHashCode();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Get_Hash_Code_Handles_Null_Components()
    {
        var address = new TestAddress("123 Main St", null);

        var hash = address.GetHashCode();

        Assert.NotEqual(0, hash);
    }

    [Fact]
    public void Operator_Equals_Returns_True_For_Equal_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        var result = address1 == address2;

        Assert.True(result);
    }

    [Fact]
    public void Operator_Equals_Returns_False_For_Different_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("456 Oak Ave", "Springfield");

        var result = address1 == address2;

        Assert.False(result);
    }

    [Fact]
    public void Operator_Equals_Returns_True_When_Both_Are_Null()
    {
        TestAddress? address1 = null;
        TestAddress? address2 = null;

        var result = address1 == address2;

        Assert.True(result);
    }

    [Fact]
    public void Operator_Equals_Returns_False_When_Left_Is_Null()
    {
        TestAddress? address1 = null;
        var address2 = new TestAddress("123 Main St", "Springfield");

        var result = address1 == address2;

        Assert.False(result);
    }

    [Fact]
    public void Operator_Equals_Returns_False_When_Right_Is_Null()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        TestAddress? address2 = null;

        var result = address1 == address2;

        Assert.False(result);
    }

    [Fact]
    public void Operator_Not_Equals_Returns_False_For_Equal_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        var result = address1 != address2;

        Assert.False(result);
    }

    [Fact]
    public void Operator_Not_Equals_Returns_True_For_Different_Values()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("456 Oak Ave", "Springfield");

        var result = address1 != address2;

        Assert.True(result);
    }

    [Fact]
    public void Operator_Not_Equals_Returns_False_When_Both_Are_Null()
    {
        TestAddress? address1 = null;
        TestAddress? address2 = null;

        var result = address1 != address2;

        Assert.False(result);
    }

    [Fact]
    public void Operator_Not_Equals_Returns_True_When_Left_Is_Null()
    {
        TestAddress? address1 = null;
        var address2 = new TestAddress("123 Main St", "Springfield");

        var result = address1 != address2;

        Assert.True(result);
    }

    [Fact]
    public void Operator_Not_Equals_Returns_True_When_Right_Is_Null()
    {
        var address1 = new TestAddress("123 Main St", "Springfield");
        TestAddress? address2 = null;

        var result = address1 != address2;

        Assert.True(result);
    }

    [Fact]
    public void Value_Object_Comparison_Respects_Component_Order()
    {
        var value1 = new TestTwoStrings("A", "B");
        var value2 = new TestTwoStrings("B", "A");

        var result = value1.Equals(value2);

        Assert.False(result);
    }

    [Fact]
    public void Value_Object_With_Multiple_Components_Compares_All()
    {
        var person1 = new TestPerson("John", "Doe", 30);
        var person2 = new TestPerson("John", "Doe", 30);
        var person3 = new TestPerson("John", "Doe", 31);

        Assert.True(person1 == person2);
        Assert.False(person1 == person3);
    }

    [Fact]
    public void Value_Object_With_Null_Components_Compares_Correctly()
    {
        var address1 = new TestAddress("123 Main St", null);
        var address2 = new TestAddress("123 Main St", null);
        var address3 = new TestAddress("123 Main St", "Springfield");

        Assert.True(address1 == address2);
        Assert.False(address1 == address3);
    }

    [Fact]
    public void Value_Object_Can_Be_Used_In_Dictionary()
    {
        var dictionary = new Dictionary<TestAddress, string>();
        var address = new TestAddress("123 Main St", "Springfield");

        dictionary[address] = "Home";

        Assert.Equal("Home", dictionary[address]);
    }

    [Fact]
    public void Value_Object_Dictionary_Lookup_Works_With_Equal_Instance()
    {
        var dictionary = new Dictionary<TestAddress, string>();
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        dictionary[address1] = "Home";

        Assert.Equal("Home", dictionary[address2]);
    }

    [Fact]
    public void Value_Object_Can_Be_Used_In_Hash_Set()
    {
        var hashSet = new HashSet<TestAddress>();
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        hashSet.Add(address1);
        hashSet.Add(address2);

        Assert.Single(hashSet);
    }

    [Fact]
    public void Value_Object_Hash_Set_Contains_Works_With_Equal_Instance()
    {
        var hashSet = new HashSet<TestAddress>();
        var address1 = new TestAddress("123 Main St", "Springfield");
        var address2 = new TestAddress("123 Main St", "Springfield");

        hashSet.Add(address1);

        Assert.Contains(address2, hashSet);
    }

    [Fact]
    public void Value_Object_With_Complex_Types_Compares_Correctly()
    {
        var order1 = new TestOrder(new TestMoney(100m, "USD"), new TestAddress("123 Main St", "Springfield"));
        var order2 = new TestOrder(new TestMoney(100m, "USD"), new TestAddress("123 Main St", "Springfield"));
        var order3 = new TestOrder(new TestMoney(100m, "USD"), new TestAddress("456 Oak Ave", "Springfield"));

        Assert.True(order1 == order2);
        Assert.False(order1 == order3);
    }

    private sealed class TestAddress(string street, string? city) : ValueObject
    {
        public string Street { get; } = street;
        public string? City { get; } = city;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    private sealed class TestMoney(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class TestTwoStrings(string first, string second) : ValueObject
    {
        public string First { get; } = first;
        public string Second { get; } = second;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return First;
            yield return Second;
        }
    }

    private sealed class TestPerson(string firstName, string lastName, int age) : ValueObject
    {
        public string FirstName { get; } = firstName;
        public string LastName { get; } = lastName;
        public int Age { get; } = age;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return FirstName;
            yield return LastName;
            yield return Age;
        }
    }

    private sealed class TestOrder(TestMoney price, TestAddress address) : ValueObject
    {
        public TestMoney Price { get; } = price;
        public TestAddress Address { get; } = address;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Price;
            yield return Address;
        }
    }
}
