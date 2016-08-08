namespace EventProcessing.BusinessObjects
{
    public class Metadata
    {
        public const string Name = "EventHandling";

        public static class ExceptionHandling
        {
            public const int BaseErrorCode = 21900;
            public const int MineCareBaseCode = 23300;

            public static class BusinessComponent
            {
                public const int Code = BaseErrorCode + 1;
            }

            public static class SharedServices
            {
                public const int Code = BaseErrorCode + 2;
            }

            public static class UnhandledException
            {
                public const int Code = BaseErrorCode;
            }

            public static class NotValidException
            {
                public const int Code = BaseErrorCode + 1;
            }

            public static class NotUniqueException
            {
                public const int Code = BaseErrorCode + 2;
            }

            public static class AlreadyRemovedException
            {
                public const int Code = BaseErrorCode + 3;
            }

            public static class NotFoundException
            {
                public const int Code = BaseErrorCode + 4;
            }

            public static class HasDependencyException
            {
                public const int Code = BaseErrorCode + 5;
            }

            public static class RelatedDataNotFoundException
            {
                public const int Code = BaseErrorCode + 6;
            }

            public static class EventSetDiagnosticRequiredException
            {
                public const int Code = BaseErrorCode + 7;
            }

            public static class EventSetAcceptanceRequiredException
            {
                public const int Code = BaseErrorCode + 8;
            }

            public static class EventSetCompleteByOwnerRequiredException
            {
                public const int Code = BaseErrorCode + 9;
            }

            public static class EventSetAlreadyAcceptedException
            {
                public const int Code = BaseErrorCode + 10;
            }

            public static class EventSetAlreadyCompletedException
            {
                public const int Code = BaseErrorCode + 11;
            }

            public static class SuppressionAlreadyExistsException
            {
                public const int Code = BaseErrorCode + 12;
            }

            public static class DbUpdateConcurrencyException
            {
                public const int Code = BaseErrorCode + 13;
            }
        }
    }
}