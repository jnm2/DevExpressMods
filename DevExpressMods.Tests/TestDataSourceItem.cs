namespace DevExpressMods.Tests
{
    internal struct TestDataSourceItem
    {
        public TestDataSourceItem(string name, double value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public double Value { get; }
    }
}