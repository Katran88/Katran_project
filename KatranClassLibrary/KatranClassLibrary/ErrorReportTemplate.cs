using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public enum ErrorType
    {
        Other, WrongLoginOrPassword, UserAlreadyRegistr
    }

    [Serializable]
    public class ErrorReportTemplate
    {
        public Exception Error;
        public ErrorType ErrorType;

        private ErrorReportTemplate()
        {
            ErrorType = ErrorType.Other;
            Error = new Exception();
        }

        public ErrorReportTemplate(ErrorType errorType, Exception error)
        {
            ErrorType = errorType;
            Error = error;
        }
    }
}
