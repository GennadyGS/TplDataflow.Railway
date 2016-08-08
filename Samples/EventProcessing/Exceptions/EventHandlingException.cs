using EventProcessing.BusinessObjects;
using System;
using System.Globalization;

namespace EventProcessing.Exceptions
{
    public class EventHandlingException : Exception
    {
        public int ErrorCode { get; set; }

        public EventHandlingException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public EventHandlingException(string message, int errorCode, Exception innerException)
            : base(message, innerException) { }

        public string Title
        {
            get { return base.Message; }
        }

        public string RecoveryStrategy
        {
            get { return string.Empty; }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        public bool IsAlreadyCompleted
        {
            get { return ErrorCode == Metadata.ExceptionHandling.EventSetAlreadyCompletedException.Code; }
        }

        public bool IsNotFound
        {
            get { return ErrorCode == Metadata.ExceptionHandling.NotFoundException.Code; }
        }

        public static EventHandlingException CreateNotFoundException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.NotFoundException.Code);
        }

        public static EventHandlingException CreateNotValidException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.NotValidException.Code);
        }

        public static EventHandlingException CreateAlreadyRemovedException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.AlreadyRemovedException.Code);
        }

        public static EventHandlingException CreateHasDependencyException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.HasDependencyException.Code);
        }

        public static EventHandlingException CreateNotUniqueException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.NotUniqueException.Code);
        }

        public static Exception CreateRelatedDataNotFoundException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.RelatedDataNotFoundException.Code);
        }

        public static Exception CreateAlreadyAccepted(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetAlreadyAcceptedException.Code);
        }

        public static Exception CreateAlreadyCompleted(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetAlreadyCompletedException.Code);
        }

        public static Exception CreateAcceptanceRequired(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetAcceptanceRequiredException.Code);
        }

        public static Exception CreateDiagnosticRequired(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetDiagnosticRequiredException.Code);
        }

        public static Exception CreateCompleteByOwnerRequired(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetCompleteByOwnerRequiredException.Code);
        }

        public static Exception CreateSuppressionExists(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.SuppressionAlreadyExistsException.Code);
        }

        private static EventHandlingException CreateException(string message, object[] args, int errorCode)
        {
            string errorMessage = message;
            if (args != null && args.Length > 0)
            {
                errorMessage = string.Format(CultureInfo.InvariantCulture, message, args);
            }

            return new EventHandlingException(errorMessage, errorCode);
        }
    }
}