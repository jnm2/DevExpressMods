using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.Design;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;

namespace DevExpressMods.Design
{
    public class FilterStringEditor : UITypeEditor
    {
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object objValue)
        {
            if (provider != null && context != null)
            {
                var serviceProvider = context.Container as IServiceProvider;
                if (serviceProvider == null)
                {
                    var componentSite = (context.Instance as IComponent)?.Site;
                    if (componentSite == null) return objValue;
                    serviceProvider = componentSite;
                }

                var rootReport = (DesignerExtensions.GetService<IDesignerHost>(serviceProvider).RootComponent as XRControl)?.RootReport;
                if (rootReport == null) return objValue;

                var dataContainer = context.Instance as IDataContainer;
                if (dataContainer == null) return objValue;

                var form = new FilterStringEditorForm(provider, dataContainer.GetEffectiveDataSource(), dataContainer.DataMember, rootReport.Parameters, rootReport)
                {
                    FilterString = (string)objValue
                };
                if (DialogRunner.ShowDialog(form, provider) != DialogResult.Cancel)
                {
                    objValue = form.FilterString;
                }
            }
            return objValue;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return context?.Instance != null ? UITypeEditorEditStyle.Modal : base.GetEditStyle(context);
        }
    }
}
