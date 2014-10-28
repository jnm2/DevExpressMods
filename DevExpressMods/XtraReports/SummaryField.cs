using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using DevExpress.Data;
using DevExpress.Data.Browsing;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.Native.Data;
using DevExpress.XtraReports.UI;
using DevExpressMods.Design;

namespace DevExpressMods.XtraReports
{
    [DXDisplayName(typeof(SummaryField), "LocalizableNames", "Techsola.Controls.Reports.SummaryField", "Summary Field")]
    public class SummaryField : CalculatedField
    {
        public SummaryField()
        {
            summary = new XRSummary();
            summary.SetControl(new XRLabel());
            base.GetValue += SummaryField_GetValue;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public GetValueEventHandler GetValue;

        private readonly XRSummary summary;
        private static readonly Action<XRSummary, object, int> addValue = typeof(XRSummary).GetMethodDelegate<Action<XRSummary, object, int>>("AddValue");
        private static readonly Action<XRSummary> reset = typeof(XRSummary).GetMethodDelegate<Action<XRSummary>>("Reset");
        private static readonly Func<XRSummary, IEnumerable> get_ValuesInfo = typeof(XRSummary).GetMethodDelegate<Func<XRSummary, IEnumerable>>("get_ValuesInfo");
        private static readonly Func<XtraReportBase, ReportDataContext> get_DataContext = typeof(XtraReportBase).GetMethodDelegate<Func<XtraReportBase, ReportDataContext>>("get_DataContext");
  
        private int lastPosition = -1;
        private object lastCollection;
        private ExpressionEvaluator expressionEvaluator;
        private Band effectiveOwner;
        private CustomSortedListController unfilteredListController;
        private IList unfilteredListControllerList;

        private class CustomSortedListController : SortedListController
        {
            public CustomSortedListController(ListBrowser listBrowser)
            {
                this.SetBrowser(listBrowser);
                this.SetList(listBrowser.List);
            }
        }

        private static readonly Func<XtraReportBase, string, CriteriaOperator> GetFilterCriteria = typeof(XtraReportBase).GetMethodDelegate<Func<XtraReportBase, string, CriteriaOperator>>("GetFilterCriteria");

        private static int GetImmediateGroupLevel(Band band)
        {
            if (band == null) return -1;
            var r = band as GroupBand;
            if (r != null) return r.Level;
            return band.Report.Bands.OfType<GroupBand>().Select(gb => gb.Level).DefaultIfEmpty(-1).Max();
        }

        private static object[] GetGroupRows(Band groupBand, ListBrowser groupDataBrowser, SortedListController groupListController)
        {
            var groupLevel = GetImmediateGroupLevel(groupBand);
            if (groupLevel == -1) return null;

            var groupDataController = groupListController.GetDataController();
            var currentControllerIndex = groupListController.GetIndicesMapper()[groupDataBrowser.Position];

            var rebasedGroupLevel = groupDataController.GroupInfo.LevelCount - groupLevel - 1;

            var groupInfo = groupDataController.GroupInfo.FirstOrDefault(i => i.Level == rebasedGroupLevel && i.ContainsControllerRow(currentControllerIndex));
            if (groupInfo == null) throw new Exception("This was unexpected for immediate mode. The summary field its running group have the same data member, but no group information is available.");

            var r = new object[groupInfo.ChildControllerRowCount];

            for (var i = 0; i < r.Length; i++)
                r[i] = groupListController.GetItem(i);

            return r;
        }

        void SummaryField_GetValue(object sender, GetValueEventArgs e)
        {
            if (effectiveOwner == null) Reset();

            var dataContext = get_DataContext(e.Report);
            var browser = (ListBrowser)dataContext.GetDataBrowser(this.DataSource ?? e.Report.DataSource, this.DataMember, true);

            SortedListController listController;
            if (this.OverrideFilter != null)
            {
                if (unfilteredListController == null || unfilteredListControllerList != browser.List)
                {
                    unfilteredListControllerList = browser.List;
                    unfilteredListController = new CustomSortedListController(browser);                    
                    unfilteredListController.Initialize(
                        ((SortedListController)browser.ListController).GetCalculatedFields(),
                        ((SortedListController)browser.ListController).GetOriginalGroupFields(),
                        ((SortedListController)browser.ListController).GetSortingSummary(),
                        GetFilterCriteria(e.Report, this.OverrideFilter));
                }
                listController = unfilteredListController;
            }
            else 
                listController = (SortedListController)browser.ListController;

            if (this.Mode == SummaryFieldMode.Immediate)
            {
                if (effectiveOwner == null)
                {
                    effectiveOwner = this.Running ?? e.Report;                    
                    effectiveOwner.BeforePrint += effectiveOwner_BeforePrint;

                    if (expressionEvaluator == null)
                        expressionEvaluator = new ExpressionEvaluator(new CalculatedEvaluatorContextDescriptor(e.Report.Parameters, this, dataContext), CriteriaOperator.TryParse(this.Expression));


                    var runningDataMember = (this.Running == null ? e.Report : this.Running.Report).DataMember;
                    if (string.Equals(runningDataMember, this.DataMember, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var groupRows = GetGroupRows(this.Running, browser, listController);
                        if (groupRows != null)
                        {
                            for (var i = 0; i < groupRows.Length; i++)
                                addValue(summary, expressionEvaluator.Evaluate(listController.GetItem(i)), i);
                        }
                        else
                        {
                            for (var i = 0; i < listController.Count; i++)
                                addValue(summary, expressionEvaluator.Evaluate(listController.GetItem(i)), i);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("In immediate mode, you cannot pick a group running band unless its data member is the same as the summary field's data member.");
                    }
                }
            }
            else
            {
                if (effectiveOwner == null)
                {
                    effectiveOwner = this.Running ?? e.Report;
                    effectiveOwner.BeforePrint += effectiveOwner_BeforePrint;

                    e.Report.BeforePrint -= Report_BeforePrint;
                    e.Report.BeforePrint += Report_BeforePrint;
                }
                
                var currentListSource = listController.GetDataController().ListSource;
                if (lastCollection != currentListSource)
                {
                    lastPosition = -1;
                    lastCollection = currentListSource;
                }

                int currentPosition;
                if (listController == browser.ListController)
                {
                    currentPosition = browser.Position;
                    if (currentPosition < lastPosition) // Charting doubles back
                    {
                        lastPosition = -1;
                        Reset();
                    }
                }
                else
                {
                    currentPosition = -1;
                    for (var i = 0; i < listController.Count; i++)
                        if (listController.GetItem(i) == browser.Current)
                        {
                            currentPosition = i;
                            break;
                        }
                }

                while (lastPosition < currentPosition)
                {
                    lastPosition++;
                    if (expressionEvaluator == null)
                        expressionEvaluator = new ExpressionEvaluator(new CalculatedEvaluatorContextDescriptor(e.Report.Parameters, this, dataContext), CriteriaOperator.TryParse(this.Expression));
                    addValue(summary, expressionEvaluator.Evaluate(listController.GetItem(lastPosition)), lastPosition);
                }
            }

            if (!get_ValuesInfo(summary).Cast<Pair<object, int>>().Any(p => p.First != null))
                e.Value = null;
            else
                e.Value = summary.GetResult();
        }

        void Report_BeforePrint(object sender, EventArgs e)
        {
            lastPosition = -1;
            var report = (XRControl)sender;
            report.BeforePrint -= Report_BeforePrint;
        }

        void effectiveOwner_BeforePrint(object sender, EventArgs e)
        {
            expressionEvaluator = null;
            effectiveOwner.BeforePrint -= effectiveOwner_BeforePrint;
            effectiveOwner = null;
        }

        private void Reset()
        {
            unfilteredListController = null;
            unfilteredListControllerList = null;
            reset(summary);
        }


        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        new public CalculatedFieldScripts Scripts { get { return null; } }

        [Category("Data"), DefaultValue(typeof(SummaryFunc), "Sum")]
        public SummaryFunc Func { get { return summary.Func; } set { summary.Func = value; } }

        [Category("Data"), DefaultValue(false), DisplayName("Ignore Null Values")]
        public bool IgnoreNullValues { get { return summary.IgnoreNullValues; } set { summary.IgnoreNullValues = value; } }

        [Category("Data"), DefaultValue(null), TypeConverter(typeof(RunningBandConverter))]
        public Band Running { get; set; }

        [Category("Data"), DefaultValue(typeof(SummaryFieldMode), "Immediate")]
        public SummaryFieldMode Mode { get; set; }

        [Category("Data"), DefaultValue(null), DisplayName("Override Filter"), Editor(typeof(FilterStringEditor), typeof(UITypeEditor))]
        public string OverrideFilter { get; set; }
    }

    public enum SummaryFieldMode
    {
        Immediate,
        Incremental        
    }

    public class RunningBandConverter : ComponentConverter
    {
        public RunningBandConverter()
            : base(typeof(Band))
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value == null)
                return "(entire report)";

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var baseValues = base.GetStandardValues(context);
            var r = new List<Band>();
            r.Add(null);

            for (var i = 0; i < baseValues.Count; i++)
            {
                var value = (Band)baseValues[i];
                if (value is GroupHeaderBand || value is DetailReportBand)
                    r.Add(value);
            }

            return new StandardValuesCollection(r);
        }
    }

    public static class ListControllerExtensions
    {
        private readonly static Func<SortedListController, ListSourceDataController> get_dataController = typeof(SortedListController).GetFieldGetter<Func<SortedListController, ListSourceDataController>>("dataController");
        public static ListSourceDataController GetDataController(this SortedListController listController)
        {
            return get_dataController(listController);
        }

        private readonly static Func<SortedListController, RowIndicesMapper> get_IndicesMapper = typeof(SortedListController).GetMethodDelegate<Func<SortedListController, RowIndicesMapper>>("get_IndicesMapper");
        public static RowIndicesMapper GetIndicesMapper(this SortedListController listController)
        {
            return get_IndicesMapper(listController);
        }

        private readonly static Func<SortedListController, CalculatedFieldCollection> get_calculatedFields = typeof(SortedListController).GetFieldGetter<Func<SortedListController, CalculatedFieldCollection>>("calculatedFields");
        public static CalculatedFieldCollection GetCalculatedFields(this SortedListController listController)
        {
            return get_calculatedFields(listController);
        }

        private readonly static Func<SortedListController, GroupField[]> get_originalGroupFields = typeof(SortedListController).GetFieldGetter<Func<SortedListController, GroupField[]>>("originalGroupFields");
        public static GroupField[] GetOriginalGroupFields(this SortedListController listController)
        {
            return get_originalGroupFields(listController);
        }

        private readonly static Func<SortedListController, XRGroupSortingSummary[]> get_sortingSummary = typeof(SortedListController).GetFieldGetter<Func<SortedListController, XRGroupSortingSummary[]>>("sortingSummary");
        public static XRGroupSortingSummary[] GetSortingSummary(this SortedListController listController)
        {
            return get_sortingSummary(listController);
        }
    }

    public static class XRSummaryExtensions
    {
        private static readonly Action<XRSummary, XRLabel> set_Control = typeof(XRSummary).GetMethodDelegate<Action<XRSummary, XRLabel>>("set_Control");

        public static void SetControl(this XRSummary @this, XRLabel value)
        {
            set_Control(@this, value);
        }
    }
}
