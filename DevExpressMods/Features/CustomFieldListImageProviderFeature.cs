using System;
using System.ComponentModel;
using DevExpress.Data.Browsing.Design;
using DevExpress.Utils;

namespace DevExpressMods.Features
{
    public static class CustomFieldListImageProviderFeature
    {
        public static CustomFieldListImageProvider Instance
        {
            get
            {
                var r = ColumnImageProvider.Instance as CustomFieldListImageProvider;
                if (r == null) ColumnImageProvider.Instance = r = new CustomFieldListImageProvider(ColumnImageProvider.Instance);
                return r;
            }
        }

        public class CustomFieldListImageProvider : IColumnImageProvider
        {
            private readonly IColumnImageProvider wrapped;
            public int FieldListSumIndex { get; private set; }
            public int FieldListForeignKeyIndex { get; private set; }

            public event EventHandler<CustomColumnImageIndexEventArgs> CustomColumnImageIndex;

            public CustomFieldListImageProvider(IColumnImageProvider wrapped)
            {
                this.wrapped = wrapped;
            }

            public ImageCollection CreateImageCollection()
            {
                var r = wrapped.CreateImageCollection();
                this.FieldListSumIndex = r.Images.Count;
                this.FieldListForeignKeyIndex = this.FieldListSumIndex + 1;
                r.AddImage(Properties.Resources.FieldListSum);
                r.AddImage(Properties.Resources.FieldListForeignKey);
                return r;
            }

            public int GetColumnImageIndex(PropertyDescriptor property, TypeSpecifics specifics)
            {
                var r = wrapped.GetColumnImageIndex(property, specifics);

                var ev = CustomColumnImageIndex;
                if (ev != null)
                {
                    var e = new CustomColumnImageIndexEventArgs(property, specifics, r);
                    ev(this, e);
                    r = e.Index;
                }

                return r;
            }

            int IColumnImageProvider.GetColumnImageIndex(PropertyDescriptor property, string dataMember, bool isList)
            {
                throw new WarningException("This method is obsolete.");
            }

            public int GetDataSourceImageIndex(object dataSource, TypeSpecifics specifics)
            {
                return wrapped.GetDataSourceImageIndex(dataSource, specifics);
            }

            int IColumnImageProvider.GetDataSourceImageIndex(object dataSource)
            {
                throw new WarningException("This method is obsolete.");
            }

            public int GetNoneImageIndex()
            {
                return wrapped.GetNoneImageIndex();
            }
        }
    }
}
