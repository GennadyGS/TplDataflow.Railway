using EventProcessing.BusinessObjects;
using System;
using System.Globalization;

namespace EventProcessing.Exceptions
{
    /// <summary>
    /// EventHandling default exception.
    /// </summary>
    public class EventHandlingException : Exception
    {
        public int ErrorCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlingException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="errorCode">The error code.</param>
        public EventHandlingException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlingException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public EventHandlingException(string message, int errorCode, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string Title
        {
            get { return base.Message; }
        }

        /// <summary>
        /// Gets the recovery strategy.
        /// </summary>
        public string RecoveryStrategy
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is already completed.
        /// </summary>
        public bool IsAlreadyCompleted
        {
            get { return ErrorCode == Metadata.ExceptionHandling.EventSetAlreadyCompletedException.Code; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is not found.
        /// </summary>
        public bool IsNotFound
        {
            get { return ErrorCode == Metadata.ExceptionHandling.NotFoundException.Code; }
        }

        /// <summary>
        /// Creates the not found exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static EventHandlingException CreateNotFoundException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.NotFoundException.Code);
        }

        /// <summary>
        /// Creates the not valid exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static EventHandlingException CreateNotValidException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.NotValidException.Code);
        }

        /// <summary>
        /// Creates the already removed exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static EventHandlingException CreateAlreadyRemovedException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.AlreadyRemovedException.Code);
        }

        /// <summary>
        /// Creates the has dependency exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static EventHandlingException CreateHasDependencyException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.HasDependencyException.Code);
        }

        /// <summary>
        /// Creates the not unique exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static EventHandlingException CreateNotUniqueException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.NotUniqueException.Code);
        }

        /// <summary>
        /// Creates the related data not found exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static Exception CreateRelatedDataNotFoundException(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.RelatedDataNotFoundException.Code);
        }

        /// <summary>
        /// Creates the already accepted exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static Exception CreateAlreadyAccepted(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetAlreadyAcceptedException.Code);
        }

        /// <summary>
        /// Creates the already completed.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static Exception CreateAlreadyCompleted(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetAlreadyCompletedException.Code);
        }

        /// <summary>
        /// Creates the acceptance required.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static Exception CreateAcceptanceRequired(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetAcceptanceRequiredException.Code);
        }

        /// <summary>
        /// Creates the diagnostic required.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static Exception CreateDiagnosticRequired(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetDiagnosticRequiredException.Code);
        }

        /// <summary>
        /// Creates the complete by owner required.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns> OEM Data Provider exception with properly filled error code. </returns>
        public static Exception CreateCompleteByOwnerRequired(string message, params object[] args)
        {
            return CreateException(message, args, Metadata.ExceptionHandling.EventSetCompleteByOwnerRequiredException.Code);
        }

        /// <summary>
        /// Creates the suppression exist.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
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