using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var dockManager = this.Container.Components.OfType<XRDesignDockManager>().First();
            SummaryFieldsFeature.Apply(this, dockManager);
        }    

        public ModdedEndUserDesigner()
            : base()
        {
        }

        public ModdedEndUserDesigner(IContainer container)
            : base(container)
        {
        }
    }
}
