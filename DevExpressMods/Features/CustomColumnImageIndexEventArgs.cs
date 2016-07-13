using System;
using System.ComponentModel;
using DevExpress.Data.Browsing.Design;

namespace DevExpressMods.Features
{
    public sealed class CustomColumnImageIndexEventArgs : EventArgs
    {
        public PropertyDescriptor Property { get; }
        public TypeSpecifics Specifics { get; }
        public int Index { get; set; }

        public CustomColumnImageIndexEventArgs(PropertyDescriptor property, TypeSpecifics specifics, int index)
        {
            Property = property;
            Specifics = specifics;
            Index = index;
        }
    }
}