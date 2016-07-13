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
using DevExpressMods.Features;
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
                var fieldList = (DataSourceNativeTreeList)fieldListPanel.GetFieldList();

                return (MenuItemDescriptionCollection)typeof(DataSourceNativeTreeList).GetMethod("CreateMenuItems", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fieldList,
                    new[] { fieldList.PickManager.FindDataMemberNode(fieldList.Nodes, dataSource, dataMember) });
            }
        }
    }
}
