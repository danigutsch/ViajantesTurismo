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
    public void Entity_Dictionary_Lookup_Works_With_Equal_Instance()
    {
        var dictionary = new Dictionary<TestEntity, string>();
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        dictionary[entity1] = "Value";

        Assert.Equal("Value", dictionary[entity2]);
    }

    [Fact]
    public void Entity_Id_Is_Immutable()
    {
        var entity = new TestEntity(1);
        var id = entity.Id;

        Assert.Equal(1, id);
    }

    [Fact]
    public void Different_Entity_Types_With_Same_Id_Type_Are_Not_Equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new AnotherTestEntity(1);

        Assert.False(entity1.Equals(entity2));
    }

    private sealed class TestEntity(int id) : Entity<int>(id);

    private sealed class TestEntityDifferentType(int id) : Entity<int>(id);

    private sealed class AnotherTestEntity(int id) : Entity<int>(id);

    private sealed class TestEntityNullableId(string? id) : Entity<string?>(id);
}
