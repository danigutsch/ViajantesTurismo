using static ViajantesTurismo.Common.UnitTests.BuildingBlocks.EntityTestsHelpers;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

// ReSharper disable EqualExpressionComparison
// ReSharper disable SuspiciousTypeConversion.Global
public sealed class EntityTests
{
    [Fact]
    public void Entity_with_same_id_are_equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        var result = entity1.Equals(entity2);

        Assert.True(result);
    }

    [Fact]
    public void Entity_with_different_ids_are_not_equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_returns_false_when_other_is_null()
    {
        var entity = new TestEntity(1);

        var result = EqualsObject(entity, null);

        Assert.False(result);
    }

    [Fact]
    public void Equals_returns_false_for_different_entity_types()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntityDifferentType(1);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_returns_false_when_other_is_not_entity()
    {
        var entity = new TestEntity(1);
        object other = "Not an entity";

        var result = entity.Equals(other);

        Assert.False(result);
    }

    [Fact]
    public void Equals_returns_false_when_id_is_default()
    {
        var entity1 = new TestEntity(0);
        var entity2 = new TestEntity(0);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_returns_false_when_one_id_is_default()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(0);

        var result = entity1.Equals(entity2);

        Assert.False(result);
    }

    [Fact]
    public void Get_hash_code_returns_same_value_for_same_id()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Get_hash_code_returns_different_values_for_different_ids()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Get_hash_code_throws_when_id_is_null()
    {
        var entity = new TestEntityNullableId(null);

        Assert.Throws<ArgumentNullException>(() => entity.GetHashCode());
    }

    [Fact]
    public void Entity_dictionary_lookup_works_with_equal_instance()
    {
        var dictionary = new Dictionary<TestEntity, string>();
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        dictionary[entity1] = "Value";

        Assert.Equal("Value", dictionary[entity2]);
    }

    [Fact]
    public void Entity_id_is_immutable()
    {
        var entity = new TestEntity(1);
        var id = entity.Id;

        Assert.Equal(1, id);
    }

    [Fact]
    public void Different_entity_types_with_same_id_type_are_not_equal()
    {
        var entity1 = new TestEntity(1);
        var entity2 = new AnotherTestEntity(1);

        Assert.False(entity1.Equals(entity2));
    }

}
