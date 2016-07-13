using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraReports.Design;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.UserDesigner;
using DevExpressMods.Design;
using DevExpressMods.Features;
using DevExpressMods.XtraReports;
using NUnit.Framework;

namespace DevExpressMods.Tests
{
    [TestFixture]
    public class TestEndUserDesigner
    {
        [Test, Apartment(ApartmentState.STA)]
        public void Add_summary_field_should_be_available_on_ribbon_designer()
        {
            Assert.That(
                GetMenuItems(typeof(TestDataSource), nameof(TestDataSource.Items), true).Select(_ => _.CommandID),
                Contains.Item(SummaryFieldsFeature.AddSummaryFieldCommand));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void Add_summary_field_should_be_available_on_old_designer()
        {
            Assert.That(
                GetMenuItems(typeof(TestDataSource), nameof(TestDataSource.Items), false).Select(_ => _.CommandID),
                Contains.Item(SummaryFieldsFeature.AddSummaryFieldCommand));
        }

        private static MenuItemDescriptionCollection GetMenuItems(Type dataSourceType, string dataMember, bool ribbonForm)
        {
            var dataSource = new BindingSource { DataSource = dataSourceType };
            using (var tool = new ReportDesignTool(new XtraReport { DataSource = dataSource }))
            {
                var form = ribbonForm ? tool.DesignRibbonForm : tool.DesignForm;
                SummaryFieldsFeature.Apply(form.DesignMdiController, form.DesignDockManager);
                form.OpenReport(tool.Report);

                var fieldListPanel = (FieldListDockPanel)form.DesignDockManager[DesignDockPanelType.FieldList];
                var fieldList = (XRDesignFieldList)fieldListPanel.DesignControl;

                return (MenuItemDescriptionCollection)typeof(DataSourceNativeTreeList).GetMethod("CreateMenuItems", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fieldList,
                    new[] { fieldList.PickManager.FindDataMemberNode(fieldList.Nodes, dataSource, dataMember) });
            }
        }


        [Test, Apartment(ApartmentState.STA)]
        public void Add_summary_field_should_have_effect_on_ribbon_designer()
        {
            AssertAddSummaryFieldCommandWorks(typeof(TestDataSource), nameof(TestDataSource.Items), true);
        }


        [Test, Apartment(ApartmentState.STA)]
        public void Add_summary_field_should_have_effect_on_old_designer()
        {
            AssertAddSummaryFieldCommandWorks(typeof(TestDataSource), nameof(TestDataSource.Items), false);
        }

        private static void AssertAddSummaryFieldCommandWorks(Type dataSourceType, string dataMember, bool ribbonForm)
        {
            var dataSource = new BindingSource { DataSource = dataSourceType };
            using (var tool = new ReportDesignTool(new XtraReport { DataSource = dataSource }))
            {
                var form = ribbonForm ? tool.DesignRibbonForm : tool.DesignForm;
                SummaryFieldsFeature.Apply(form.DesignMdiController, form.DesignDockManager);
                form.OpenReport(tool.Report);

                var fieldListPanel = (FieldListDockPanel)form.DesignDockManager[DesignDockPanelType.FieldList];
                var fieldList = (XRDesignFieldList)fieldListPanel.DesignControl;
                fieldList.SelectDataMemberNode(dataSource, dataMember);
                fieldList.Selection.Set(new[] { fieldList.FocusedNode });

                var menuCommandService = form.DesignMdiController.ActiveDesignPanel.GetService<IMenuCommandService>();
                menuCommandService.GlobalInvoke(SummaryFieldsFeature.AddSummaryFieldCommand);
                Assert.That(tool.Report.CalculatedFields.Count, Is.EqualTo(1));
            }
        }


        [Test, Apartment(ApartmentState.STA)]
        public void Summary_field_should_have_correct_icon_on_ribbon_designer()
        {
            const string summaryFieldName = "SummaryField";
            Assert.That(
                GetFieldListIcon(typeof(TestDataSource), $"{nameof(TestDataSource.Items)}.{summaryFieldName}", new CalculatedField[]
                {
                    new SummaryField { DataMember = nameof(TestDataSource.Items), Name = summaryFieldName }
                }, true),
                Is.EqualTo(CustomFieldListImageProviderFeature.Instance.FieldListSumIndex));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void Summary_field_should_have_correct_icon_on_old_designer()
        {
            const string summaryFieldName = "SummaryField";
            Assert.That(
                GetFieldListIcon(typeof(TestDataSource), $"{nameof(TestDataSource.Items)}.{summaryFieldName}", new CalculatedField[]
                {
                    new SummaryField { DataMember = nameof(TestDataSource.Items), Name = summaryFieldName }
                }, false),
                Is.EqualTo(CustomFieldListImageProviderFeature.Instance.FieldListSumIndex));
        }

        private static int GetFieldListIcon(Type dataSourceType, string dataMember, CalculatedField[] calculatedFields, bool ribbonForm)
        {
            var dataSource = new BindingSource { DataSource = dataSourceType };

            using (var report = new XtraReport { DataSource = dataSource })
            using (var tool = new ReportDesignTool(report))
            {
                report.CalculatedFields.AddRange(calculatedFields);

                var form = ribbonForm ? tool.DesignRibbonForm : tool.DesignForm;
                SummaryFieldsFeature.Apply(form.DesignMdiController, form.DesignDockManager);
                form.OpenReport(tool.Report);

                var fieldListPanel = (FieldListDockPanel)form.DesignDockManager[DesignDockPanelType.FieldList];
                var fieldList = (XRDesignFieldList)fieldListPanel.DesignControl;
                return ((DataMemberListNode)fieldList.PickManager.FindDataMemberNode(fieldList.Nodes, dataSource, dataMember)).StateImageIndex;
            }
        }
    }
}
