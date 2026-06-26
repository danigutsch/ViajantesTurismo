using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

internal sealed class TestEntityDifferentType(int id) : Entity<int>(id);
