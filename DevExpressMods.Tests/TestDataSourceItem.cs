using System.Collections.Generic;

namespace DevExpressMods.Tests
{
    internal sealed class TestDataSourceItem
    {
        public TestDataSourceItem(string name, double value, IEnumerable<TestDataSourceItem> recursiveItems = null)
        {
            Name = name;
            Value = value;
            if (recursiveItems != null)
                foreach (var item in recursiveItems)
                    RecursiveItems.Add(item);
        }

        public string Name { get; }
        public double Value { get; }

        public ICollection<TestDataSourceItem> RecursiveItems { get; } = new List<TestDataSourceItem>();
    }
}