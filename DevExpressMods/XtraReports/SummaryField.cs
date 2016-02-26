using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using DevExpress.Data;
using DevExpress.Data.Browsing;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.Native.Data;
using DevExpress.XtraReports.Native.Parameters;
using DevExpress.XtraReports.UI;
using DevExpressMods.Design;

namespace DevExpressMods.XtraReports
{
    [DXDisplayName(typeof(SummaryField), "LocalizableNames", "DevExpressMods.XtraReports.SummaryField", "Summary Field")]
    public class SummaryField : CalculatedField
    {
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new GetValueEventHandler GetValue;

        private readonly XRSummary summary;
        private int lastPosition = -1;
        private object lastCollection;
        private ExpressionEvaluator expressionEvaluator;
        private ExpressionEvaluator overrideFilterEvaluator;
        private Band effectiveOwner;
        private CustomSortedListController unfilteredListController;
        private IList unfilteredListControllerList;



        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new CalculatedFieldScripts Scripts => null;

        [Category("Data"), DefaultValue(typeof(SummaryFunc), nameof(SummaryFunc.Sum))]
        public SummaryFunc Func { get { return summary.Func; } set { summary.Func = value; } }

        [Category("Data"), DefaultValue(false), DisplayName("Ignore Null Values")]
        public bool IgnoreNullValues { get { return summary.IgnoreNullValues; } set { summary.IgnoreNullValues = value; } }

        [Category("Data"), DefaultValue(null), TypeConverter(typeof(RunningBandConverter))]
        public Band Running { get; set; }

        [Category("Data"), DefaultValue(typeof(SummaryFieldMode), nameof(SummaryFieldMode.Immediate))]
        public SummaryFieldMode Mode { get; set; }

        [Category("Data"), DefaultValue(null), DisplayName("Override Filter"), Editor(typeof(FilterStringEditor), typeof(UITypeEditor))]
        public string OverrideFilter
        {
            get { return overrideFilter; }
            set
            {
                value = ParametersReplacer.UpgradeFilterString(value); // See XtraReportBase.FilterString
                if (overrideFilter == value) return;
                overrideFilter = value;
                overrideFilterEvaluator = null;
            }
        }

        public SummaryField()
        {
            summary = new XRSummary();
            set_Control(summary, new XRLabel());
            base.GetValue += SummaryField_GetValue;
        }




        private static readonly Action<XRSummary, object, int> addValue = typeof(XRSummary).GetMethodDelegate<Action<XRSummary, object, int>>("AddValue");
        private static readonly Action<XRSummary> reset = typeof(XRSummary).GetMethodDelegate<Action<XRSummary>>("Reset");
        private static readonly Func<XRSummary, IEnumerable> get_ValuesInfo = typeof(XRSummary).GetMethodDelegate<Func<XRSummary, IEnumerable>>("get_ValuesInfo");
        private static readonly Func<XtraReportBase, ReportDataContext> get_DataContext = typeof(XtraReportBase).GetMethodDelegate<Func<XtraReportBase, ReportDataContext>>("get_DataContext");
        private static readonly Func<XtraReportBase, string, CriteriaOperator> GetFilterCriteria = typeof(XtraReportBase).GetMethodDelegate<Func<XtraReportBase, string, CriteriaOperator>>("GetFilterCriteria");
        private static readonly Action<XRSummary, XRLabel> set_Control = typeof(XRSummary).GetMethodDelegate<Action<XRSummary, XRLabel>>("set_Control");

        private sealed class CustomSortedListController : SortedListController
        {
            public new void SetList(IList list)
            {
                base.SetList(list);
            }
        }

        private struct DataGroupInfo : IEquatable<DataGroupInfo>
        {
            public readonly object DataSource;
            public readonly string DataMember;
            public readonly ListBrowser ListBrowser;
            public readonly IList List;
            public readonly GroupRowInfo GroupRowInfo;

            public DataGroupInfo(object dataSource, string dataMember, ListBrowser listBrowser, IList list, GroupRowInfo groupRowInfo)
            {
                if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
                if (dataMember == null) throw new ArgumentNullException(nameof(dataMember));
                ListBrowser = listBrowser;
                List = list;
                GroupRowInfo = groupRowInfo;
                DataSource = dataSource;
                DataMember = dataMember;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is DataGroupInfo && Equals((DataGroupInfo)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (uint)DataSource.GetHashCode();
                    hashCode = (hashCode * 397) ^ (uint)DataMember.GetHashCode();
                    hashCode *= 397;
                    if (GroupRowInfo != null) hashCode ^= (uint)GroupRowInfo.ChildControllerRow;
                    hashCode *= 397;
                    if (GroupRowInfo != null) hashCode ^= (uint)GroupRowInfo.ChildControllerRowCount;
                    return (int)hashCode;
                }
            }

            public bool Equals(DataGroupInfo other)
            {
                return DataSource.Equals(other.DataSource)
                    && string.Equals(DataMember, other.DataMember)
                    && other.List == List
                    && (GroupRowInfo == null
                        ? other.GroupRowInfo == null
                        : GroupRowInfo.ChildControllerRow == other.GroupRowInfo.ChildControllerRow && GroupRowInfo.ChildControllerRowCount == other.GroupRowInfo.ChildControllerRowCount);
            }
        }

        private DataGroupInfo? GetCurrentDataGroup(ReportDataContext dataContext, Band runningBand)
        {
            if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
            if (runningBand == null) throw new ArgumentNullException(nameof(runningBand));

            var closestMember = runningBand as XtraReportBase ?? runningBand.GetParentBoundReportBand();
            var browser = (ListBrowser)dataContext.GetDataBrowser(closestMember.DataSource, closestMember.DataMember, true);
            if (browser == null) return null;

            var groupBand = runningBand as GroupBand;
            if (groupBand != null)
            {
                var groupRowLevel = DataGroupingUtils.GetGroupRowLevel((SortedListController)browser.ListController, groupBand.Level);
                if (groupRowLevel != null)
                {
                    return new DataGroupInfo(
                        closestMember.DataSource,
                        closestMember.DataMember,
                        browser,
                        browser.List,
                        DataGroupingUtils.GetGroupInfo(browser, groupRowLevel.Value, browser.Position));
                }
                runningBand = closestMember;
            }

            var detailBand = runningBand as XtraReportBase;
            if (detailBand != null)
            {
                return new DataGroupInfo(closestMember.DataSource, closestMember.DataMember, browser, browser.List, null);
            }

            throw new InvalidOperationException($"Running band '{runningBand.Name}' cannot be used for immediate mode grouping. Calculated field {Name} must choose a {nameof(GroupBand)} or a {nameof(DetailReportBand)}.");
        }




        private DataGroupInfo? lastDataGroupInfo;
        private bool isImmediateSummaryValid;
        private object immediateSummary;
        private string overrideFilter;

        private bool ConvertOverrideFilterResult(object result)
        {
            if (result is bool) return (bool)result;
            throw new EndUserConfigurationException($"Summary field {Name} has an invalid override filter because it returned a value other than boolean true or false.\r\rTry using comparison operators (=, !=, <, etc).");
        }


        void report_AfterPrint(object sender, EventArgs e)
        {
            var report = (XtraReport)sender;
            report.AfterPrint -= report_AfterPrint;

            // Force recreate on next get_summary because data Source may have changed (workaround for casting error)
            expressionEvaluator = null;
            overrideFilterEvaluator = null;

            // This is important because changing parameters may affect summaries
            isImmediateSummaryValid = false;
        }

        private object GetImmediateModeSummary(XtraReport report)
        {
            report.AfterPrint -= report_AfterPrint;
            report.AfterPrint += report_AfterPrint;

            var dataContext = get_DataContext(report);

            if (expressionEvaluator == null)
            {
                isImmediateSummaryValid = false;
                expressionEvaluator = new ExpressionEvaluator(new CalculatedEvaluatorContextDescriptor(report.Parameters, this, dataContext), CriteriaOperator.Parse(Expression));
            }

            var isOverridingFilter = !string.IsNullOrWhiteSpace(OverrideFilter);
            if (isOverridingFilter && overrideFilterEvaluator == null)
            {
                isImmediateSummaryValid = false;
                overrideFilterEvaluator = new ExpressionEvaluator(new CalculatedEvaluatorContextDescriptor(report.Parameters, this, dataContext), GetFilterCriteria(report, OverrideFilter));
            }


            reset(summary);

            var dg = GetCurrentDataGroup(dataContext, Running ?? report);

            if (isImmediateSummaryValid && !dg.Equals(lastDataGroupInfo)) isImmediateSummaryValid = false;
            lastDataGroupInfo = dg;

            if (isImmediateSummaryValid) return immediateSummary;

            if (dg != null)
            {
                if (dg.Value.DataSource != (DataSource ?? report.DataSource))
                    throw new EndUserConfigurationException($"The running band on summary field {Name} does not have the same data Source as the summary field. Otherwise, it is not able to be correlated with the summary field.");
                if (!DataMemberUtils.AreEqual(DataMember, dg.Value.DataMember) && (!DataMemberUtils.IsAncestor(dg.Value.DataMember, DataMember) || string.IsNullOrEmpty(dg.Value.DataMember)))
                    throw new EndUserConfigurationException($"The running band on summary field {Name} must have the same data member or must be a parent data member. Otherwise, it is not able to be correlated with the summary field.");

                var groupListBrowser = dg.Value.ListBrowser;
                var childBrowsers = DataMemberUtils.GetChildBrowsers(dataContext, dg.Value.DataSource, dg.Value.DataMember, DataMember);

                var groupStart = dg.Value.GroupRowInfo == null ? 0 : dg.Value.GroupRowInfo.ChildControllerRow;
                var groupRowCount = dg.Value.GroupRowInfo == null ? groupListBrowser.Count : dg.Value.GroupRowInfo.ChildControllerRowCount;

                if (isOverridingFilter)
                {
                    var originalBrowser = childBrowsers.Length == 0 ? groupListBrowser : childBrowsers[childBrowsers.Length - 1];
                    var originalBrowserAsChild = originalBrowser as IRelatedDataBrowser;

                    var newListController = new CustomSortedListController();
                    newListController.SetList(originalBrowser.List);
                    var newBrowser = originalBrowserAsChild != null
                        ? (ListBrowser)new CustomRelatedListBrowser((DataBrowser)originalBrowserAsChild.Parent, originalBrowserAsChild.RelatedProperty, newListController, false)
                        : new CustomListBrowser(originalBrowser.DataSource, newListController, false);
                    ((IPropertiesContainer)newBrowser).SetCustomProperties(originalBrowser.GetItemProperties().OfType<CalculatedPropertyDescriptorBase>().Cast<PropertyDescriptor>().ToArray());
                    newListController.SetBrowser(newBrowser);

                    // Filter outside the list controller so that we can ascertain grouping for rows the override filter excludes
                    newListController.Initialize(
                        ((SortedListController)originalBrowser.ListController).GetOriginalGroupFields(),
                        ((SortedListController)originalBrowser.ListController).GetSortingSummary(),
                        null);

                    if (childBrowsers.Length == 0)
                    {
                        groupListBrowser = newBrowser;
                        if (dg.Value.GroupRowInfo != null)
                        {
                            var currentListSourceIndex = ((SortedListController)originalBrowser.ListController).GetDataController().GetListSourceRowIndex(originalBrowser.Position);
                            var currentPositionNewBrowser = newListController.GetDataController().GetControllerRow(currentListSourceIndex);
                            var newGroupInfo = DataGroupingUtils.GetGroupInfo(newBrowser, dg.Value.GroupRowInfo.Level, currentPositionNewBrowser);
                            groupStart = newGroupInfo.ChildControllerRow;
                            groupRowCount = newGroupInfo.ChildControllerRowCount;
                        }
                        else
                        {
                            groupStart = 0;
                            groupRowCount = newBrowser.Count;
                        }
                    }
                    else
                    {
                        childBrowsers[childBrowsers.Length - 1] = (RelatedListBrowser)newBrowser;
                    }
                }

                if (childBrowsers.Length == 0)
                {
                    for (var i = 0; i < groupRowCount; i++)
                    {
                        var currentRow = groupListBrowser.GetRow(i + groupStart);
                        if (!isOverridingFilter || ConvertOverrideFilterResult(overrideFilterEvaluator.Evaluate(currentRow)))
                            addValue(summary, expressionEvaluator.Evaluate(currentRow), i);
                    }
                }
                else
                {
                    var state = dataContext.SaveState();
                    try
                    {
                        groupListBrowser.Position = groupStart;
                        var sampleIndex = 0;
                        foreach (var childBrowser in childBrowsers) childBrowser.Position = 0;

                        while (true)
                        {
                            var currentIndex = childBrowsers.Length - 1;
                            var currentBrowser = childBrowsers[currentIndex];
                            if (!isOverridingFilter || ConvertOverrideFilterResult(overrideFilterEvaluator.Evaluate(currentBrowser.Current)))
                                addValue(summary, expressionEvaluator.Evaluate(currentBrowser.Current), sampleIndex);
                            sampleIndex++;

                            while (currentIndex > 0 && currentBrowser.Position == currentBrowser.Count - 1)
                            {
                                currentBrowser.Position = 0;
                                currentIndex--;
                                currentBrowser = childBrowsers[currentIndex];
                            }

                            if (currentBrowser.Position == currentBrowser.Count - 1)
                            {
                                if (groupListBrowser.Position == groupStart + groupRowCount - 1) break;
                                groupListBrowser.Position++;
                            }
                            else
                                currentBrowser.Position++;
                        }
                    }
                    finally
                    {
                        dataContext.LoadState(state);
                    }
                }
            }

            immediateSummary = get_ValuesInfo(summary).Cast<Pair<object, int>>().All(p => p.First == null) ? null : summary.GetResult();
            isImmediateSummaryValid = true;
            return immediateSummary;
        }

        private object GetIncrementalModeSummary(XtraReport report)
        {
            if (effectiveOwner == null) Reset();

            var dataContext = get_DataContext(report);
            var browser = (ListBrowser)dataContext.GetDataBrowser(DataSource ?? report.DataSource, DataMember, false);
            if (browser == null) return null;

            SortedListController listController;
            if (OverrideFilter != null)
            {
                if (unfilteredListController == null || unfilteredListControllerList != browser.List)
                {
                    unfilteredListControllerList = browser.List;
                    unfilteredListController = new CustomSortedListController();
                    unfilteredListController.SetList(browser.List);
                    unfilteredListController.SetBrowser(browser);
                    unfilteredListController.Initialize(
                        ((SortedListController)browser.ListController).GetOriginalGroupFields(),
                        ((SortedListController)browser.ListController).GetSortingSummary(),
                        GetFilterCriteria(report, OverrideFilter));
                }
                listController = unfilteredListController;
            }
            else
                listController = (SortedListController)browser.ListController;


            if (effectiveOwner == null)
            {
                effectiveOwner = Running ?? report;
                effectiveOwner.BeforePrint += effectiveOwner_BeforePrint;

                report.BeforePrint -= Report_BeforePrint;
                report.BeforePrint += Report_BeforePrint;
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
                    expressionEvaluator = new ExpressionEvaluator(new CalculatedEvaluatorContextDescriptor(report.Parameters, this, dataContext), CriteriaOperator.TryParse(Expression));
                addValue(summary, expressionEvaluator.Evaluate(listController.GetItem(lastPosition)), lastPosition);
            }

            return get_ValuesInfo(summary).Cast<Pair<object, int>>().All(p => p.First == null) ? null : summary.GetResult();
        }

        void SummaryField_GetValue(object sender, GetValueEventArgs e)
        {
            switch (Mode)
            {
                case SummaryFieldMode.Immediate:
                    e.Value = GetImmediateModeSummary(e.Report);
                    break;
                case SummaryFieldMode.Incremental:
                    e.Value = GetIncrementalModeSummary(e.Report);
                    break;
                default:
                    throw new InvalidEnumValueException(Mode);
            }
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
            overrideFilterEvaluator = null;
            effectiveOwner.BeforePrint -= effectiveOwner_BeforePrint;
            effectiveOwner = null;
        }

        private void Reset()
        {
            unfilteredListController = null;
            unfilteredListControllerList = null;
            reset(summary);
        }
    }
}