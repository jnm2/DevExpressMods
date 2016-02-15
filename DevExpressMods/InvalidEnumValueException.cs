using System;

namespace DevExpressMods
{
    public class InvalidEnumValueException : Exception
    {
        public InvalidEnumValueException(object value)
            : base($"{value.GetType().Name} '{value}' is not a valid value.")
        {
        }
    }
}
