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
        private static object GetDefaultDataSource() => new TestDataSource(new[]
        {
            new TestDataSourceItem("Item 1", 1),
            new TestDataSourceItem("Item 2", 10),
            new TestDataSourceItem("Item 3", 100)
        });

        [Test]
        public void TestImmediateModeSimple()
        {
            Assert.That(GetPrintedValues(
                GetDefaultDataSource(),
                nameof(TestDataSource.Items),
                new SummaryField
                {
                    Name = "SummaryCalculation",
                    Expression = $"[{nameof(TestDataSourceItem.Value)}]",
                    DataSource = null,
                    DataMember = nameof(TestDataSource.Items),
                    Mode = SummaryFieldMode.Immediate,
                    Func = SummaryFunc.Sum
                }), Has.All.EqualTo("111"));
        }

        [Test]
        public void TestIncrementalModeSimple()
        {
            Assert.That(GetPrintedValues(
                GetDefaultDataSource(),
                nameof(TestDataSource.Items), 
                new SummaryField
                {
                    Name = "SummaryCalculation",
                    Expression = $"[{nameof(TestDataSourceItem.Value)}]",
                    DataSource = null,
                    DataMember = nameof(TestDataSource.Items),
                    Mode = SummaryFieldMode.Incremental,
                    Func = SummaryFunc.Sum
                }), Is.EqualTo(new[]
                {
                    "1",
                    "11",
                    "111"
                }));
        }

        private static object GetDataSourceForEmptyDataMember() => new[]
        {
            new TestDataSourceItem("Item 1", 1),
            new TestDataSourceItem("Item 2", 10),
            new TestDataSourceItem("Item 3", 100)
        };

        [Test]
        public void TestImmediateModeEmptyDataMember()
        {
            Assert.That(GetPrintedValues(
                GetDataSourceForEmptyDataMember(),
                null,
                new SummaryField
                {
                    Name = "SummaryCalculation",
                    Expression = $"[{nameof(TestDataSourceItem.Value)}]",
                    DataSource = null,
                    DataMember = null,
                    Mode = SummaryFieldMode.Immediate,
                    Func = SummaryFunc.Sum
                }), Has.All.EqualTo("111"));
        }



        private static IList<string> GetPrintedValues(object dataSource, string dataMember, SummaryField testField)
        {
            var label = new XRLabel { DataBindings = { { nameof(XRLabel.Text), null, $"{testField.DataMember}.{testField.Name}" } } };

            using (var report = new XtraReport
            {
                DataSource = dataSource,
                DataMember = dataMember,
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
