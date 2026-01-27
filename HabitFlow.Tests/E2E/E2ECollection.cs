using HabitFlow.Tests.E2E.Infrastructure;
using Xunit;

namespace HabitFlow.Tests.E2E;

[CollectionDefinition("E2E", DisableParallelization = true)]
public sealed class E2ECollection : ICollectionFixture<E2EFixture>
{
}
