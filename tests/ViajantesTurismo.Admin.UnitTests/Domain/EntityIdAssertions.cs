using System.Reflection;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class EntityIdAssertions
{
    public static void AssertUuidV7(Guid id)
    {
        var guidText = id.ToString("D");
        (guidText[14]).ShouldBe('7');
    }

    public static void SetId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var nonNullIdProperty = (idProperty).ShouldNotBeNull();
        var idSetter = nonNullIdProperty.GetSetMethod(nonPublic: true);
        var nonNullIdSetter = (idSetter).ShouldNotBeNull();

        nonNullIdSetter.Invoke(entity, [id]);
    }

    public static void AssertGeneratedIdentitySemantics<T>(T first, T second, T different)
        where T : class
    {
        var id = Guid.CreateVersion7();
        SetId(first, id);
        SetId(second, id);
        SetId(different, Guid.CreateVersion7());
        var set = new HashSet<T> { first };

        (EqualityComparer<T>.Default.Equals(first, first)).ShouldBeTrue();
        (EqualityComparer<T>.Default.Equals(first, second)).ShouldBeTrue();
        (EqualityComparer<T>.Default.Equals(first, different)).ShouldBeFalse();
        (EqualityComparer<T>.Default.Equals(first, null)).ShouldBeFalse();
        var firstHashCode = first is null ? 0 : first.GetHashCode();
        var secondHashCode = second is null ? 0 : second.GetHashCode();
        (secondHashCode).ShouldBe(firstHashCode);
        (set).ShouldContain(second);
        (set).ShouldNotContain(different);

        SetId(second, Guid.Empty);

        (EqualityComparer<T>.Default.Equals(first, second)).ShouldBeFalse();
        (EqualityComparer<T>.Default.Equals(second, first)).ShouldBeFalse();
        (set).ShouldNotContain(second);
    }
}
