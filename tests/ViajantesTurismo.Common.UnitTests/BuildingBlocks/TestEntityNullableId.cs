using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

internal sealed class TestEntityNullableId(string? id) : Entity<string?>(id);
