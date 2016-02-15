using DevExpress.XtraReports.UI;

namespace DevExpressMods.XtraReports
{
    public static class ReportExtensions
    {
        public static XtraReportBase GetParentBoundReportBand(this XRControl xrControl)
        {
            var currentBand = xrControl as Band ?? xrControl.Band;

            while (true)
            {
                if (currentBand == null) return null;

                var detailReportBand = currentBand as XtraReportBase;
                if (detailReportBand != null && (detailReportBand.DataSource != null || detailReportBand.DataMember != null))
                    return detailReportBand;

                currentBand = currentBand.Parent as Band;
            }
        }
    }
}
