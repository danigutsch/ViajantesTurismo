namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class EntityIdAssertions
{
    public static void AssertUuidV7(Guid id)
    {
        var guidText = id.ToString("D");
        Assert.Equal('7', guidText[14]);
    }
}
