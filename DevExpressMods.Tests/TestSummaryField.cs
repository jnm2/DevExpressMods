using System.Collections.Generic;
using System.Linq;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using DevExpressMods.XtraReports;
using NUnit.Framework;

namespace DevExpressMods.Tests
{
    [TestFixture]
    public class TestSummaryField
    {
        private sealed class TestDataSource
        {
            public TestDataSource(IReadOnlyCollection<TestDataSourceItem> items)
            {
                Items = items;
            }

            public IReadOnlyCollection<TestDataSourceItem> Items { get; }
        }

        private struct TestDataSourceItem
        {
            public TestDataSourceItem(string name, double value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public double Value { get; }
        }

        [Test]
        public void TestImmediateModeSimple()
        {
            Assert.That(GetPrintedValues(new SummaryField
            {
                Mode = SummaryFieldMode.Immediate,
                Func = SummaryFunc.Sum
            }), Has.All.EqualTo("111"));
        }

        [Test]
        public void TestIncrementalModeSimple()
        {
            Assert.That(GetPrintedValues(new SummaryField
            {
                Mode = SummaryFieldMode.Incremental,
                Func = SummaryFunc.Sum
            }), Is.EqualTo(new[]
            {
                "1",
                "11",
                "111"
            }));
        }

        private static IReadOnlyList<string> GetPrintedValues(SummaryField testField)
        {
            const string summaryFieldName = "SummaryCalculation";

            testField.Name = summaryFieldName;
            testField.Expression = $"[{nameof(TestDataSourceItem.Value)}]";
            testField.DataSource = null;
            testField.DataMember = nameof(TestDataSource.Items);

            var label = new XRLabel { DataBindings = { { nameof(XRLabel.Text), null, $"{nameof(TestDataSource.Items)}.{summaryFieldName}" } } };

            using (var report = new XtraReport
            {
                DataSource = new TestDataSource(new[]
                {
                    new TestDataSourceItem("Item 1", 1),
                    new TestDataSourceItem("Item 2", 10),
                    new TestDataSourceItem("Item 3", 100)
                }),
                DataMember = nameof(TestDataSource.Items),

                CalculatedFields = { testField },

                Bands = { new DetailBand { Controls = { label } } }
            })
            {
                report.CreateDocument();

                return report.PrintingSystem.Document.Pages.SelectMany(_ => _.InnerBricks).SelectManyRecursive(_ => (_ as CompositeBrick)?.InnerBricks)
                    .OfType<LabelBrick>()
                    .Where(_ => _.BrickOwner == label)
                    .Select(_ => _.Text)
                    .ToList();
            }
        }
    }
}
