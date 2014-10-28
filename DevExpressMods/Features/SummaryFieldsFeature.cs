using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
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
    public class CustomColumnImageIndexEventArgs : EventArgs
    {
        public PropertyDescriptor Property { get; private set; }
        public TypeSpecifics Specifics { get; private set; }
        public int Index { get; set; }

        public CustomColumnImageIndexEventArgs(PropertyDescriptor property, TypeSpecifics specifics, int index)
        {
            this.Property = property;
            this.Specifics = specifics;
            this.Index = index;
        }
    }

    public static class SummaryFieldsFeature
    {
        public static void Apply(XRDesignMdiController designMdiController, XRDesignDockManager designDockManager)
        {
            CustomFieldListImageProviderFeature.Instance.CustomColumnImageIndex -= CustomFieldListImageProviderFeature_CustomColumnImageIndex;
            CustomFieldListImageProviderFeature.Instance.CustomColumnImageIndex += CustomFieldListImageProviderFeature_CustomColumnImageIndex;

            if (designMdiController.ActiveDesignPanel == null)
                designMdiController.DesignPanelLoaded += designMdiController_DesignPanelLoaded;
            else
                RefreshFieldListImages(designDockManager);

            MenuCreationServiceContainer.Get(designMdiController).Add(new SummaryFieldsMenuCreationService(designMdiController, designDockManager));
        }


        static void CustomFieldListImageProviderFeature_CustomColumnImageIndex(object sender, CustomColumnImageIndexEventArgs e)
        {
            var containerComponent = e.Property as CalculatedPropertyDescriptorBase;
            if (containerComponent != null && ((IContainerComponent)containerComponent).Component is SummaryField)
                e.Index = CustomFieldListImageProviderFeature.Instance.FieldListSumIndex;
        }

        static void designMdiController_DesignPanelLoaded(object sender, DesignerLoadedEventArgs e)
        {
            var designPanel = (XRDesignPanel)sender;
            var designMdiController = designPanel.GetDesignMdiController();
            designMdiController.DesignPanelLoaded -= designMdiController_DesignPanelLoaded;
            RefreshFieldListImages(designMdiController.Container.Components.OfType<XRDesignDockManager>().First());
        }
        
        private static void RefreshFieldListImages(XRDesignDockManager designDockManager)
        {
            ((FieldListDockPanel)designDockManager[DesignDockPanelType.FieldList]).GetFieldList().StateImageList = ColumnImageProvider.Instance.CreateImageCollection();
        }

        private class SummaryFieldsMenuCreationService : IMenuCreationService
        {
            private readonly XRDesignMdiController designMdiController;
            private readonly XRDesignDockManager designDockManager;

            public SummaryFieldsMenuCreationService(XRDesignMdiController designMdiController, XRDesignDockManager designDockManager)
            {
                this.designMdiController = designMdiController;
                this.designDockManager = designDockManager;
                addSummaryFieldCommand = new CommandID(Guid.NewGuid(), 0);
            }

            readonly CommandID addSummaryFieldCommand;
            public MenuCommandDescription[] GetCustomMenuCommands()
            {
                return new[]
                {
                    new MenuCommandDescription(addSummaryFieldCommand, OnHandleAddSummaryField, OnStatusAddSummaryField)
                };
            }

            private void OnHandleAddSummaryField(object sender, CommandExecuteEventArgs e)
            {
                var designPanel = designMdiController.ActiveDesignPanel;
                var fieldListControl = ((FieldListDockPanel)designDockManager[DesignDockPanelType.FieldList]).GetFieldList();
                var node = fieldListControl.DataMemberNode;
                if (node == null) return;
                if (!node.IsList) node = (DataMemberListNodeBase)node.ParentNode;

                var report = designPanel.Report;
                var designerHost = designPanel.GetService<IDesignerHost>();
                var changeServ = designPanel.GetService<IComponentChangeService>();
                var selectionServ = designPanel.GetService<ISelectionService>();

                // Functionality patterned after AddCalculatedField() from DevExpress.XtraReports.Design.Commands.FieldListCommandExecutor, DevExpress.XtraReports.v14.1.Extensions.dll, Version=14.1.5.0 
                var c = new SummaryField { DataSource = node.DataSource != report.DataSource ? node.DataSource : null, DataMember = node.DataMember ?? string.Empty };
                var description = string.Format("Add {0} object", typeof(SummaryField).Name);
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

            private void OnStatusAddSummaryField(object sender, EventArgs e)
            {
                var command = sender as MenuCommand;
                command.Supported = command.Enabled = true;
            }

            public void ProcessMenuItems(MenuKind menuKind, MenuItemDescriptionCollection items)
            {
                if (menuKind == MenuKind.FieldList)
                {
                    var index = items.IndexOf(FieldListCommands.AddCalculatedField);
                    if (index != -1)
                        items.Insert(index + 1, new MenuItemDescription("Add Summary Field", null, addSummaryFieldCommand));
                }
            }
        }
    }    

    public static class FieldListDockPanelExtensions
    {
        private readonly static Func<FieldListDockPanel, XRDesignFieldList> get_fieldList = typeof(FieldListDockPanel).GetFieldGetter<Func<FieldListDockPanel, XRDesignFieldList>>("fieldList");
        public static XRDesignFieldList GetFieldList(this FieldListDockPanel @this)
        {
            return get_fieldList(@this);
        }
    }
}
