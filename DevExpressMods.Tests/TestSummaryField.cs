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

                return GetPrintedInstances(report, label);
            }
        }

        private static List<string> GetPrintedInstances(XtraReport report, XRControl label)
        {
            return report.PrintingSystem.Document.Pages.SelectMany(_ => _.InnerBricks).SelectManyRecursive(_ => (_ as CompositeBrick)?.InnerBricks)
                .OfType<LabelBrick>()
                .Where(_ => _.BrickOwner == label)
                .Select(_ => _.Text)
                .ToList();
        }



        [Test, Timeout(2000)]
        public void Immediate_empty_child_collection_grouping_by_parent()
        {
            var testField = new SummaryField
            {
                Name = "SummaryCalculation",
                Expression = $"[{nameof(TestDataSourceItem.Value)}]",
                DataMember = $"{nameof(TestDataSource.Items)}.{nameof(TestDataSourceItem.RecursiveItems)}",
                Mode = SummaryFieldMode.Immediate,
                Func = SummaryFunc.Sum
            };

            var label = new XRLabel { DataBindings = { { nameof(XRLabel.Text), null, $"{testField.DataMember}.{testField.Name}" } } };
            var dataSource = new TestDataSource(new[]
            {
                new TestDataSourceItem("Item 1", 1),
                new TestDataSourceItem("Item 2", 10, new[]
                {
                    new TestDataSourceItem("Item 2.1", 2),
                    new TestDataSourceItem("Item 2.2", 20)
                })
            });

            using (var report = new XtraReport
            {
                DataSource = dataSource,
                DataMember = nameof(TestDataSource.Items),
                CalculatedFields = { testField },
                Bands =
                {
                    new DetailBand(),
                    new DetailReportBand
                    {
                        DataSource = dataSource,
                        DataMember = $"{nameof(TestDataSource.Items)}.{nameof(TestDataSourceItem.RecursiveItems)}",
                        Bands = { new DetailBand { Controls = { label } } },
                        ReportPrintOptions = { DetailCountOnEmptyDataSource = 0 }
                    }
                }
            })
            {
                report.CreateDocument();
                Assert.That(GetPrintedInstances(report, label), Has.All.EqualTo("22"));
            }
        }
        


        [Test]
        public void Filtering_by_summary_field()
        {
            var testField = new SummaryField
            {
                Name = "SummaryCalculation",
                Expression = $"[{nameof(TestDataSourceItem.Value)}]",
                DataMember = nameof(TestDataSource.Items),
                Mode = SummaryFieldMode.Immediate,
                Func = SummaryFunc.Avg,
                FieldType = FieldType.Int32
            };

            var label = new XRLabel { DataBindings = { { nameof(XRLabel.Text), null, $"{nameof(TestDataSource.Items)}.{nameof(TestDataSourceItem.Value)}" } } };

            using (var report = new XtraReport
            {
                DataSource = GetDefaultDataSource(),
                DataMember = nameof(TestDataSource.Items),
                CalculatedFields = { testField },
                Bands = { new DetailBand { Controls = { label } } },
                FilterString = $"[{nameof(TestDataSourceItem.Value)}] < [{testField.Name}]"
            })
            {
                report.CreateDocument();
                Assert.That(GetPrintedInstances(report, label), Is.EqualTo(new[]
                {
                    "1",
                    "10"
                }));
            }
        }
    }
}
