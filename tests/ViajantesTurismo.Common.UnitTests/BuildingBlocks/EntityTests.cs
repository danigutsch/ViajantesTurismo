using JetBrains.Annotations;
using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

#pragma warning disable CA1508
// ReSharper disable EqualExpressionComparison
// ReSharper disable SuspiciousTypeConversion.Global
public sealed class EntityTests
{
    [Fact]
    public void Entity_With_Same_Id_Are_Equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        var result = entity1.Equals(entity2);

        Assert.True(result);
    }

    [Fact]
    public void Entity_With_Different_Ids_Are_Not_Equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_True_For_Same_Instance()
    {
        var entity = new TestEntity(1);

        var result = entity.Equals(entity);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Returns_False_When_Other_Is_Null()
    {
        var entity = new TestEntity(1);

        var result = entity.Equals(null);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_False_For_Different_Entity_Types()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntityDifferentType(1);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_False_When_Other_Is_Not_Entity()
    {
        var entity = new TestEntity(1);
        object other = "Not an entity";

        var result = entity.Equals(other);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_False_When_Id_Is_Default()
    {
        var entity1 = new TestEntity(0);
        var entity2 = new TestEntity(0);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Returns_False_When_One_Id_Is_Default()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(0);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Get_Hash_Code_Returns_Same_Value_For_Same_Id()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Get_Hash_Code_Returns_Different_Values_For_Different_Ids()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Get_Hash_Code_Throws_When_Id_Is_Null()
    {
        var entity = new TestEntityNullableId(null);

        Assert.Throws<ArgumentNullException>(() => entity.GetHashCode());
    }

    [Fact]
    public void Entity_Can_Be_Used_In_Dictionary()
    {
        var dictionary = new Dictionary<TestEntity, string>();
        var entity = new TestEntity(1);

        dictionary[entity] = "Value";

        Assert.Equal("Value", dictionary[entity]);
    }

    [Fact]
    public void Entity_Dictionary_Lookup_Works_With_Equal_Instance()
    {
        var dictionary = new Dictionary<TestEntity, string>();
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        dictionary[entity1] = "Value";

        Assert.Equal("Value", dictionary[entity2]);
    }

    [Fact]
    public void Entity_Can_Be_Used_In_Hash_Set()
    {
        var hashSet = new HashSet<TestEntity>();
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        hashSet.Add(entity1);
        hashSet.Add(entity2);

        Assert.Single(hashSet);
    }

    [Fact]
    public void Entity_Hash_Set_Contains_Works_With_Equal_Instance()
    {
        var hashSet = new HashSet<TestEntity>();
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        hashSet.Add(entity1);

        Assert.Contains(entity2, hashSet);
    }

    [Fact]
    public void Entity_With_String_Id_Works_Correctly()
    {
        var entity1 = new TestEntityStringId("ABC123");
        var entity2 = new TestEntityStringId("ABC123");

        Assert.True(entity1.Equals(entity2));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_With_Guid_Id_Works_Correctly()
    {
        var guid = Guid.NewGuid();
        var entity1 = new TestEntityGuidId(guid);
        var entity2 = new TestEntityGuidId(guid);

        Assert.True(entity1.Equals(entity2));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_With_Long_Id_Works_Correctly()
    {
        var entity1 = new TestEntityLongId(123456789L);
        var entity2 = new TestEntityLongId(123456789L);

        Assert.True(entity1.Equals(entity2));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_Id_Is_Immutable()
    {
        var entity = new TestEntity(1);
        var id = entity.Id;

        Assert.Equal(1, id);
    }

    [Fact]
    public void Entity_Can_Be_Created_With_Parameterless_Constructor()
    {
        var entity = new TestEntityWithParameterlessConstructor();

        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public void Entity_Equality_Is_Identity_Based_Not_Value_Based()
    {
        var entity1 = new TestEntityWithProperties(1, "Name1");
        var entity2 = new TestEntityWithProperties(1, "Name2");

        Assert.True(entity1.Equals(entity2));
    }

    [Fact]
    public void Different_Entity_Types_With_Same_Id_Type_Are_Not_Equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new AnotherTestEntity(1);

        Assert.False(entity1.Equals(entity2));
    }

    [Fact]
    public void Entity_Equality_Comparison_Is_Type_Safe()
    {
        var entity = new TestEntity(1);
        object number = 1;

        var result = entity.Equals(number);

        Assert.False(result);
    }

    private sealed class TestEntity(int id) : Entity<int>(id);

    private sealed class TestEntityDifferentType(int id) : Entity<int>(id);

    private sealed class AnotherTestEntity(int id) : Entity<int>(id);

    private sealed class TestEntityNullableId(string? id) : Entity<string?>(id);

    private sealed class TestEntityStringId(string id) : Entity<string>(id);

    private sealed class TestEntityGuidId(Guid id) : Entity<Guid>(id);

    private sealed class TestEntityLongId(long id) : Entity<long>(id);

    private sealed class TestEntityWithParameterlessConstructor : Entity<int>
    {
    }

    private sealed class TestEntityWithProperties(int id, string name) : Entity<int>(id)
    {
        [UsedImplicitly] public string Name { get; } = name;
    }
}
