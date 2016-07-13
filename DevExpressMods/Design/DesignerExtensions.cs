using System;
using DevExpress.XtraReports.UserDesigner;

namespace DevExpressMods.Design
{
    public static class DesignerExtensions
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }        

        public static XRDesignMdiController GetDesignMdiController(this XRDesignPanel designPanel)
        {
            return designPanel.GetService(typeof(XRDesignPanel).Assembly.GetType("DevExpress.XtraReports.UserDesigner.Native.INestedServiceProvider")) as XRDesignMdiController;
        }
    }
}
