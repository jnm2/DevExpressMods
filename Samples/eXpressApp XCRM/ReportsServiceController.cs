using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ReportsV2.Win;
using DevExpressMods.Features;

namespace XCRM.Module
{
    public class ReportsServiceController : Controller
    {
        private WinReportServiceController winReportsServiceController;

        protected override void OnActivated()
        {
            base.OnActivated();
            winReportsServiceController = Frame.GetController<WinReportServiceController>();
            winReportsServiceController.DesignFormCreated += winReportsServiceController_DesignFormCreated;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            winReportsServiceController.DesignFormCreated -= winReportsServiceController_DesignFormCreated;
        }

        private static void winReportsServiceController_DesignFormCreated(object sender, DesignFormEventArgs e)
        {
            SummaryFieldsFeature.Apply(e.DesignForm);
        }
    }
}