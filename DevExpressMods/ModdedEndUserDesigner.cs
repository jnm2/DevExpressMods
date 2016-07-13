using System.ComponentModel;
using System.Linq;
using DevExpress.XtraReports.UserDesigner;
using DevExpressMods.Features;

namespace DevExpressMods
{    
    [ToolboxItem(true)]
    public class ModdedEndUserDesigner : XRDesignMdiController, ISupportInitialize
    {
        public void BeginInit()
        {
        }

        public void EndInit()
        {
            var dockManager = Container.Components.OfType<XRDesignDockManager>().First();
            SummaryFieldsFeature.Apply(this, dockManager);
        }    

        public ModdedEndUserDesigner()
        {
        }
        public ModdedEndUserDesigner(IContainer container) : base(container)
        {
        }
    }
}
