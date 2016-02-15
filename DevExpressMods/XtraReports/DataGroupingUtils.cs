using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data;
using DevExpress.Data.Browsing;
using DevExpress.XtraReports.Native.Data;
using DevExpress.XtraReports.UI;

namespace DevExpressMods.XtraReports
{
    public static class DataGroupingUtils
    {
        /*
        ## What XtraReports does

        The process is triggered by a call to XtraReportBase.InitializeSortedController():
         > Call XtraReportBase.CollectGroupFields with XtraReportBase.Groups and the XtraReportBase's detail band as detailBand
            > Call GroupConverter.GetGroupFields with XtraReportBase.Groups
               > Effect: XtraReportBase.Groups.Where(_ => _.Header != null).Reverse().SelectMany(_ => _.Header.GroupFields)
            > Take result
            > If detailBand?.SortFields != null, add detailBand.SortFields to the result
         > Call SortedListController.Initialize with the result
            > Set result to SortedListController.originalGroupFields
            > Call SortedListController.UpdateCore()
               > Call SortedListController.ComposeGroupFields with SortedListController.originalGroupFields
                  > Effect: groups all consecutive fields that share the same non-null header into CompositeGroupFields with random names
               > Take result as groupFields
               > Call InitializeProperties which adds new random names for newly created CompositeGroupFields
               > Call SortedListController.GroupData with groupFields
                  > Call SortedListController.CreateSortInfo with groupFields
                     > Effect: (from groupField in groupFields
                                let columnInfo = dataController.Columns[_.FieldName]
                                where columnInfo != null
                                select new DataColumnSortInfo(columnInfo, (ColumnSortOrder)groupField.SortOrder)
                               ).Distinct()
                  > Take result as dataColumnSortInfo
                  > Call dataController.UpdateSortGroup with dataColumnSortInfo as sortInfo
                     > Copies sortInfo into DataController.SortInfo
                     > Calls DataController.EndUpdate
                        > Eventually calls DataController.DoGroupColumn with a new copy of DataController.SortInfo as sortInfo
                           > DoGroupColumn recursively defines a level starting at 0 and uses it to link the item at sortInfo[level] with GroupRowInfo.Level as it creates GroupRowInfos.

        Breakpoint in DevExpress.Data.GroupRowInfoCollection.CreateGroupRowInfo initally helped see it happen.



        ## Shortest path to determine which GroupRowInfo.Level matches a header band


        Problem! Since SortedListController.ComposeGroupFields gives CompositeGroupFields random names, SortedListController.ComposeGroupFields is not repeatable.
        So this won't work:

            var composedGroupFieldName =
                SortedListController.ComposeGroupFields(SortedListController.dataController, SortedListController.originalGroupFields)
                .Where(_ => _.Band?.Level >= resetBand.Level)
                .MinBy(_ => _.Band.Level)
                .FieldName;

                var groupRowInfoLevel = DataController.SortInfo.IndexOf(_ => _.FieldName == composedGroupFieldName);

                But the result of the previous call to SortedListController.ComposeGroupFields is not stored anywhere.It's immediately converted to DataColumnSortInfo, losing the band info.

        It's more efficient simply to copy the logic, tracing what we need, but this makes us slightly more dependent on DevExpress's implementation details. We're so dependent already with SummaryField that we may as well go for efficiency.
        */
        private static readonly Func<GroupField, Band> get_Band = typeof(GroupField).GetMethodDelegate<Func<GroupField, Band>>("get_Band");
        private static readonly Func<SortedListController, GroupField[]> get_originalGroupFields = typeof(SortedListController).GetFieldGetter<Func<SortedListController, GroupField[]>>("originalGroupFields");
        private struct ComposedGroupFieldInfo
        {
            public GroupField GroupField { get; }
            public bool IsComposite => GroupField == null;

            public static ComposedGroupFieldInfo Composite { get; } = default(ComposedGroupFieldInfo);

            public ComposedGroupFieldInfo(GroupField groupField)
            {
                if (groupField == null) throw new ArgumentNullException(nameof(groupField));
                GroupField = groupField;
            }
        }
        public static byte? GetGroupRowLevel(SortedListController controller, int reportBandLevel)
        {
            // Compare to SortedListController.ComposeGroupFields
            var composedFields = new List<ComposedGroupFieldInfo>();
            var lastBand = (GroupHeaderBand)null;
            var lastCountWithNonNullLevel = 0;
            foreach (var field in get_originalGroupFields.Invoke(controller))
            {
                var headerBand = get_Band.Invoke(field) as GroupHeaderBand;
                if (headerBand == null)
                {
                    composedFields.Add(new ComposedGroupFieldInfo(field));
                }
                else if (lastBand != headerBand)
                {
                    if (headerBand.Level < reportBandLevel) break;
                    composedFields.Add(new ComposedGroupFieldInfo(field));
                    lastCountWithNonNullLevel = composedFields.Count;
                }
                else
                    composedFields[composedFields.Count - 1] = ComposedGroupFieldInfo.Composite;
                lastBand = headerBand;
            }

            composedFields.RemoveRange(lastCountWithNonNullLevel, composedFields.Count - lastCountWithNonNullLevel);

            // At this point, either the list is empty or at the end of the list is a ComposedGroupFieldInfo with ReportBandLevel no less than reportBandLevel.
            // That last item is the composed field by which the report is actually sorting when you put a summary on reportBandLevel's group.
            // All that remains is to count the number of levels to get to that field.


            // Compare to SortedListController.CreateSortInfo
            var dataController = controller.GetDataController();
            var hashSet = new HashSet<Tuple<int, bool>>();
            var level = -1;
            foreach (var field in composedFields)
            {
                if (!field.IsComposite)
                {
                    var columnInfo = dataController.Columns[field.GroupField.FieldName];
                    if (columnInfo == null || !hashSet.Add(Tuple.Create(columnInfo.Index, field.GroupField.SortOrder == XRColumnSortOrder.Ascending)))
                    {
                        continue;
                    }
                }

                // Assign the next level to the field
                level++;
            }

            return level == -1 ? null : (byte?)level;
        }


        public static GroupRowInfo GetGroupInfo(ListBrowser browser, byte rowGroupLevel, int browserPosition)
        {
            var listController = (SortedListController)browser.ListController;
            var dataController = listController.GetDataController();

            // All tests I could come up with indicate that browser position and controller row are the same value.
            var r = dataController.GroupInfo.Single(i => i.Level == rowGroupLevel && i.ContainsControllerRow(browserPosition));

            /* Has never happened, in production for months.
            var sanityTestIndicesMapper = listController.GetIndicesMapper();
            for (var i = 0; i < r.ChildControllerRowCount; i++)
                if (sanityTestIndicesMapper[r.ChildControllerRow + i] != r.ChildControllerRow + i)
                    throw new InvalidOperationException("Sanity check failed on list controller indices mapper.");
            */

            return r;
        }
    }
}