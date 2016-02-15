using System;

namespace DevExpressMods.XtraReports
{
    public class EndUserConfigurationException : Exception
    {
        public EndUserConfigurationException(string message)
            : base(message)
        {
        }
    }
}