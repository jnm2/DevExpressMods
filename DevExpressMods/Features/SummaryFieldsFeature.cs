using System;
using System.ComponentModel.Design;
using DevExpress.Data.Browsing;
using DevExpress.Data.Browsing.Design;
using DevExpress.Services.Internal;
using DevExpress.XtraReports.Design;
using DevExpress.XtraReports.Design.Commands;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UserDesigner;
using DevExpressMods.Design;
using DevExpressMods.XtraReports;

namespace DevExpressMods.Features
{
    public static class SummaryFieldsFeature
    {
        public static void Apply(IDesignForm designForm) => Apply(designForm.DesignMdiController, designForm.DesignDockManager);

        public static void Apply(XRDesignMdiController designMdiController, XRDesignDockManager designDockManager)
        {
            CustomFieldListImageProviderFeature.Instance.CustomColumnImageIndex -= CustomFieldListImageProviderFeature_CustomColumnImageIndex;
            CustomFieldListImageProviderFeature.Instance.CustomColumnImageIndex += CustomFieldListImageProviderFeature_CustomColumnImageIndex;

            if (designMdiController.ActiveDesignPanel == null)
            {
                DesignerLoadedEventHandler handler = null;
                designMdiController.DesignPanelLoaded += handler = (s, e) =>
                {
                    designMdiController.DesignPanelLoaded -= handler;
                    RefreshFieldListImages(designDockManager);
                };
            }
            RefreshFieldListImages(designDockManager);
            
            MenuCreationServiceContainer.Get(designMdiController).Add(new SummaryFieldsMenuCreationService(designMdiController, designDockManager));
        }


        private static void CustomFieldListImageProviderFeature_CustomColumnImageIndex(object sender, CustomColumnImageIndexEventArgs e)
        {
            if (((IContainerComponent)(e.Property as CalculatedPropertyDescriptorBase))?.Component is SummaryField)
                e.Index = CustomFieldListImageProviderFeature.Instance.FieldListSumIndex;
        }

        private static void RefreshFieldListImages(XRDesignDockManager designDockManager)
        {
            var fieldListPanel = (FieldListDockPanel)designDockManager[DesignDockPanelType.FieldList];
            ((XRDesignFieldList)fieldListPanel.DesignControl).StateImageList = ColumnImageProvider.Instance.CreateImageCollection();
        }


        public static readonly CommandID AddSummaryFieldCommand = new CommandID(Guid.NewGuid(), 0);

        private sealed class SummaryFieldsMenuCreationService : IMenuCreationService
        {
            private readonly XRDesignMdiController designMdiController;
            private readonly XRDesignDockManager designDockManager;

            public SummaryFieldsMenuCreationService(XRDesignMdiController designMdiController, XRDesignDockManager designDockManager)
            {
                this.designMdiController = designMdiController;
                this.designDockManager = designDockManager;
            }

            public MenuCommandDescription[] GetCustomMenuCommands() => new[]
            {
                new MenuCommandDescription(AddSummaryFieldCommand, OnHandleAddSummaryField, OnStatusAddSummaryField)
            };

            private void OnHandleAddSummaryField(object sender, CommandExecuteEventArgs e)
            {
                var designPanel = designMdiController.ActiveDesignPanel;
                var fieldListPanel = (FieldListDockPanel)designDockManager[DesignDockPanelType.FieldList];
                var fieldList = (XRDesignFieldList)fieldListPanel.DesignControl;
                var node = fieldList.DataMemberNode;
                if (node == null) return;
                if (!node.IsList) node = (DataMemberListNodeBase)node.ParentNode;

                var report = designPanel.Report;
                var designerHost = designPanel.GetService<IDesignerHost>();
                var changeServ = designPanel.GetService<IComponentChangeService>();
                var selectionServ = designPanel.GetService<ISelectionService>();

                // Functionality patterned after AddCalculatedField() from DevExpress.XtraReports.Design.Commands.FieldListCommandExecutor, DevExpress.XtraReports.v16.1.Extensions.dll, Version=16.1.4.0 
                var c = new SummaryField { DataSource = node.DataSource != report.DataSource ? node.DataSource : null, DataMember = node.DataMember ?? string.Empty };
                var description = $"Add {nameof(SummaryField)} object";
                var transaction = designerHost.CreateTransaction(description);
                try
                {
                    var propertyDescriptor = XRAccessor.GetPropertyDescriptor(report, "CalculatedFields");
                    changeServ.OnComponentChanging(report, propertyDescriptor);
                    DesignToolHelper.AddToContainer(designerHost, c);
                    report.CalculatedFields.Add(c);
                    changeServ.OnComponentChanged(report, propertyDescriptor, null, null);
                }
                finally
                {
                    transaction.Commit();
                }
                selectionServ.SetSelectedComponents(new[] { c });
            }

            private static void OnStatusAddSummaryField(object sender, EventArgs e)
            {
                var command = (MenuCommand)sender;
                command.Supported = command.Enabled = true;
            }

            public void ProcessMenuItems(MenuKind menuKind, MenuItemDescriptionCollection items)
            {
                if (menuKind == MenuKind.FieldList)
                {
                    var index = items.IndexOf(FieldListCommands.AddCalculatedField);
                    if (index != -1)
                        items.Insert(index + 1, new MenuItemDescription("Add Summary Field", null, AddSummaryFieldCommand));
                }
            }
        }
    }
}
