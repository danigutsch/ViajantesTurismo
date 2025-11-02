using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ResultStatusTests
{
    [Fact]
    public void ResultStatus_Has_Correct_Values()
    {
        Assert.Equal(0, (int)ResultStatus.Unknown);
        Assert.Equal(200, (int)ResultStatus.Ok);
        Assert.Equal(201, (int)ResultStatus.Created);
        Assert.Equal(202, (int)ResultStatus.Accepted);
        Assert.Equal(204, (int)ResultStatus.NoContent);
        Assert.Equal(400, (int)ResultStatus.Invalid);
        Assert.Equal(401, (int)ResultStatus.Unauthorized);
        Assert.Equal(403, (int)ResultStatus.Forbidden);
        Assert.Equal(404, (int)ResultStatus.NotFound);
        Assert.Equal(409, (int)ResultStatus.Conflict);
        Assert.Equal(422, (int)ResultStatus.Error);
        Assert.Equal(500, (int)ResultStatus.CriticalError);
        Assert.Equal(503, (int)ResultStatus.Unavailable);
    }

    [Theory]
    [InlineData("Unknown", ResultStatus.Unknown)]
    [InlineData("Ok", ResultStatus.Ok)]
    [InlineData("Created", ResultStatus.Created)]
    [InlineData("Accepted", ResultStatus.Accepted)]
    [InlineData("NoContent", ResultStatus.NoContent)]
    [InlineData("Invalid", ResultStatus.Invalid)]
    [InlineData("Unauthorized", ResultStatus.Unauthorized)]
    [InlineData("Forbidden", ResultStatus.Forbidden)]
    [InlineData("NotFound", ResultStatus.NotFound)]
    [InlineData("Conflict", ResultStatus.Conflict)]
    [InlineData("Error", ResultStatus.Error)]
    [InlineData("CriticalError", ResultStatus.CriticalError)]
    [InlineData("Unavailable", ResultStatus.Unavailable)]
    public void ResultStatus_Can_Be_Parsed_From_String(string input, ResultStatus expected)
    {
        Assert.True(Enum.TryParse<ResultStatus>(input, out var result));
        Assert.Equal(expected, result);
    }
}
