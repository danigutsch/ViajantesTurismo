namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogIdentityAssertions
{
    public static void SetId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id");
        Assert.NotNull(idProperty);

        idProperty.SetValue(entity, id);
    }

    public static void AssertGeneratedIdentitySemantics<T>(T first, T second, T different)
        where T : class
    {
        var id = Guid.CreateVersion7();
        SetId(first, id);
        SetId(second, id);
        SetId(different, Guid.CreateVersion7());
        var set = new HashSet<T> { first };

        Assert.True(first.Equals(first));
        Assert.True(first.Equals(second));
        Assert.False(first.Equals(different));
        Assert.False(first.Equals(new object()));
        Assert.False(first.Equals(null));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Contains(second, set);
        Assert.DoesNotContain(different, set);

        SetId(second, Guid.Empty);

        Assert.False(first.Equals(second));
        Assert.False(second.Equals(first));
        Assert.DoesNotContain(second, set);
    }
}
