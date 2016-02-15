using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using DevExpress.XtraReports.UI;

namespace DevExpressMods.XtraReports
{
    public sealed class RunningBandConverter : ComponentConverter
    {
        public RunningBandConverter()
            : base(typeof(Band))
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
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
}