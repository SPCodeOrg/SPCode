using System;

namespace Lysis
{
    public class OpCodeNotKnownException : Exception
    {
        public OpCodeNotKnownException(string message) : base(message)
        {
        }
    }

    public class LogicChainConversionException : Exception
    {
        public LogicChainConversionException(string message) : base(message)
        {
        }
    }
}
