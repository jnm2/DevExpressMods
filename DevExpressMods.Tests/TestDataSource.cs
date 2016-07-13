using System.Collections.Generic;

namespace DevExpressMods.Tests
{
    internal sealed class TestDataSource
    {
        public TestDataSource(IReadOnlyCollection<TestDataSourceItem> items)
        {
            Items = items;
        }

        public IReadOnlyCollection<TestDataSourceItem> Items { get; }
    }
}