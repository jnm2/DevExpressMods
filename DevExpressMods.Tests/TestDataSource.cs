using System.Collections.Generic;

namespace DevExpressMods.Tests
{
    internal sealed class TestDataSource
    {
        public TestDataSource(ICollection<TestDataSourceItem> items)
        {
            Items = items;
        }

        public ICollection<TestDataSourceItem> Items { get; }
    }
}