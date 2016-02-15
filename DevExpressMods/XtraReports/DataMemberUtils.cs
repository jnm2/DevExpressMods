using System;
using System.Collections.Generic;
using DevExpress.Data.Browsing;
using DevExpress.XtraReports.Native.Data;

namespace DevExpressMods.XtraReports
{
    public static class DataMemberUtils
    {
        public static bool AreEqual(string dataMember1, string dataMember2)
        {
            return string.Equals(dataMember1, dataMember2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAncestor(string dataMember, string childDataMember)
        {
            if (string.IsNullOrEmpty(childDataMember)) return false;
            if (string.IsNullOrEmpty(dataMember)) return true;
            return childDataMember.Length > dataMember.Length && childDataMember.StartsWith(dataMember, StringComparison.OrdinalIgnoreCase) && childDataMember[dataMember.Length] == '.';
        }

        public static RelatedListBrowser[] GetChildBrowsers(ReportDataContext dataContext, object dataSource, string parentDataMember, string childDataMember)
        {
            if (string.IsNullOrEmpty(parentDataMember)) throw new ArgumentException("Parent data member must not be null or empty.", nameof(parentDataMember));
            if (childDataMember.Length <= parentDataMember.Length) return new RelatedListBrowser[0];

            var r = new List<RelatedListBrowser>();

            RelatedListBrowser relatedListBrowser;
            var i = parentDataMember.Length;
            while (true)
            {
                i = childDataMember.IndexOf('.', i + 1);
                if (i == -1) break;
                relatedListBrowser = dataContext.GetDataBrowser(dataSource, childDataMember.Substring(0, i), false) as RelatedListBrowser;
                if (relatedListBrowser != null) r.Add(relatedListBrowser);
            }

            relatedListBrowser = dataContext.GetDataBrowser(dataSource, childDataMember, false) as RelatedListBrowser;
            if (relatedListBrowser != null) r.Add(relatedListBrowser);

            return r.ToArray();
        }
    }
}