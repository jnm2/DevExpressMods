using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Linq;
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
            if (provider != null)
            {
                var serviceProvider = context.Container as IServiceProvider;
                if (serviceProvider == null)
                {
                    var component = context.Instance as IComponent;
                    if (component == null || component.Site == null) return objValue;
                    serviceProvider = component.Site;
                }
                var designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                var control = designerHost.RootComponent as XRControl;
                if (control == null) return objValue;
                var rootReport = control.RootReport;
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
            if ((context != null) && (context.Instance != null))
                return UITypeEditorEditStyle.Modal;
            return base.GetEditStyle(context);
        }
    }
}
