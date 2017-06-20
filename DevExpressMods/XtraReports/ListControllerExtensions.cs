using System;
using DevExpress.Data;
using DevExpress.XtraReports.Native.Data;
using DevExpress.XtraReports.UI;

namespace DevExpressMods.XtraReports
{
    public static class ListControllerExtensions
    {
        private static readonly Func<SortedListController, ListSourceDataController> get_dataController = typeof(SortedListController).GetFieldGetter<Func<SortedListController, ListSourceDataController>>("dataController");
        public static ListSourceDataController GetDataController(this SortedListController listController)
        {
            return get_dataController(listController);
        }

        private static readonly Func<SortedListController, RowIndicesMapper> get_IndicesMapper = typeof(SortedListController).GetMethodDelegate<Func<SortedListController, RowIndicesMapper>>("get_IndicesMapper");
        public static RowIndicesMapper GetIndicesMapper(this SortedListController listController)
        {
            return get_IndicesMapper(listController);
        }

        private static readonly Func<SortedListController, IGroupField[]> get_originalGroupFields = typeof(SortedListController).GetFieldGetter<Func<SortedListController, IGroupField[]>>("originalGroupFields");
        public static IGroupField[] GetOriginalGroupFields(this SortedListController listController)
        {
            return get_originalGroupFields(listController);
        }

        private static readonly Func<SortedListController, XRGroupSortingSummary[]> get_sortingSummary = typeof(SortedListController).GetFieldGetter<Func<SortedListController, XRGroupSortingSummary[]>>("sortingSummary");
        public static XRGroupSortingSummary[] GetSortingSummary(this SortedListController listController)
        {
            return get_sortingSummary(listController);
        }
    }
}
