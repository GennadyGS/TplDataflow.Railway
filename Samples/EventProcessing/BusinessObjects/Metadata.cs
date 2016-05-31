namespace EventProcessing.BusinessObjects
{
    public class Metadata
    {
        public const string Name = "EventHandling";

        /// <summary>
        /// Exception Handling specific section
        /// </summary>
        public static class ExceptionHandling
        {
            public const int BaseErrorCode = 21900;
            public const int MineCareBaseCode = 23300;

            /// <summary>
            /// Generic EventHandling exception.
            /// </summary>
            public static class BusinessComponent
            {
                public const int Code = BaseErrorCode + 1;
            }

            /// <summary>
            /// Represents error Code information for SharedServices.
            /// </summary>
            public static class SharedServices
            {
                public const int Code = BaseErrorCode + 2;
            }

            /// <summary>
            /// Represents error Code information for UnhandledException.
            /// </summary>
            /// <remarks>
            /// Message = "An unhandled exception occurred in the application configuration module."
            /// Description = "An unhandled exception occurred in the application configuration module. Reason: {0}"
            /// RecoveryStrategy = "Please, contact the System Administrator."
            /// </remarks>
            public static class UnhandledException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode;
            }

            /// <summary>
            /// Represents error Code information for NotValidException exception.
            /// </summary>
            /// <remarks>
            /// Message = "A modified entity(ies) has invalid state, some fields of the entity are in incorrect state."
            /// Description = "A modified entity(ies) has invalid state, some fields of the entity are in incorrect state."
            /// RecoveryStrategy = ""
            /// </remarks>
            public static class NotValidException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 1;
            }

            /// <summary>
            /// Represents error Code information for NotUniqueException exception.
            /// </summary>
            /// <remarks>
            /// Message = "A modified entity is not unique according to some criteria."
            /// Description = "A modified entity is not unique according to some criteria."
            /// RecoveryStrategy = ""
            /// </remarks>
            public static class NotUniqueException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 2;
            }

            /// <summary>
            /// Represents error Code information for AlreadyRemovedException exception.
            /// </summary>
            /// <remarks>
            /// Message = "A modified entity was already deleted by another user."
            /// Description = "A modified entity was already deleted by another user."
            /// RecoveryStrategy = ""
            /// </remarks>
            public static class AlreadyRemovedException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 3;
            }

            /// <summary>
            /// Represents error Code information for NotFoundException exception.
            /// </summary>
            /// <remarks>
            /// Message = "A requested data was not found."
            /// Description = "A requested data was not found."
            /// RecoveryStrategy = ""
            /// </remarks>
            public static class NotFoundException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 4;
            }

            /// <summary>
            /// Represents error Code information for HasDependencyException exception.
            /// </summary>
            /// <remarks>
            /// Message = "Occurs when entity cannot be deleted, because it has dependencies."
            /// Description = "Occurs when entity cannot be deleted, because it has dependencies."
            /// RecoveryStrategy = ""
            /// </remarks>
            public static class HasDependencyException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 5;
            }

            /// <summary>
            /// Represents error Code information for RelatedDataNotFoundException exception.
            /// </summary>
            /// <remarks>
            /// Message = "Occurs when some related data was removed."
            /// Description = "Occurs when some related data was removed."
            /// RecoveryStrategy = ""
            /// </remarks>
            public static class RelatedDataNotFoundException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 6;
            }

            /// <summary>
            /// The event set diagnostic required exception.
            /// </summary>
            public static class EventSetDiagnosticRequiredException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 7;
            }

            /// <summary>
            /// The event set acceptance required exception.
            /// </summary>
            public static class EventSetAcceptanceRequiredException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 8;
            }

            /// <summary>
            /// The event set complete by owner required exception.
            /// </summary>
            public static class EventSetCompleteByOwnerRequiredException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 9;
            }

            /// <summary>
            /// The event set already accepted exception.
            /// </summary>
            public static class EventSetAlreadyAcceptedException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 10;
            }

            public static class EventSetAlreadyCompletedException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 11;
            }

            /// <summary>
            /// Suppression Already exist exception
            /// </summary>
            public static class SuppressionAlreadyExistsException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 12;
            }

            /// <summary>
            /// Database concurrency exception
            /// </summary>
            public static class DbUpdateConcurrencyException
            {
                /// <summary>
                /// Error code.
                /// </summary>
                public const int Code = BaseErrorCode + 13;
            }
        }
    }
}