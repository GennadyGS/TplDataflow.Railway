using AsyncProcessing.Core;
using AsyncProcessing.Dataflow;
using AsyncProcessing.Dataflow.TplDataflow;
using AsyncProcessing.TplDataflow;
using Collection.Extensions;
using Dataflow.Core;
using Dataflow.Railway;
using EventProcessing.BusinessObjects;
using EventProcessing.Exceptions;
using EventProcessing.Interfaces;
using log4net;
using Railway.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LanguageExt;
using TplDataflow.Linq;
using TplDataflow.Railway;
using static LanguageExt.Prelude;
using EnumerableExtensions = Railway.Linq.EnumerableExtensions;

namespace EventProcessing.Implementation
{
    public class EventSetStorageProcessor
    {
        public enum ResultCode
        {
            EventSetCreated,
            EventSetUpdated,
            EventSkipped,
            EventFailed
        }

        public interface IFactory
        {
            IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(Func<IEventSetRepository> repositoryResolver,
                IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager,
                IEventSetConfiguration configuration, Func<DateTime> currentTimeProvider);
        }

        public class Result
        {
            private readonly EventSetWithEvents _eventSetCreated;
            private readonly EventSetWithEvents _eventSetUpdated;
            private readonly EventDetails _eventSkipped;
            private readonly EventDetails _eventFailed;

            private Result(ResultCode resultCode,
                EventSetWithEvents eventSetCreated, EventSetWithEvents eventSetUpdated,
                EventDetails eventSkipped, EventDetails eventFailed)
            {
                ResultCode = resultCode;
                _eventSetCreated = eventSetCreated;
                _eventSetUpdated = eventSetUpdated;
                _eventSkipped = eventSkipped;
                _eventFailed = eventFailed;
            }

            public ResultCode ResultCode { get; }

            public EventSetWithEvents EventSetCreated
            {
                get
                {
                    if (ResultCode != ResultCode.EventSetCreated)
                    {
                        throw new InvalidOperationException(
                            $"Trying to get EventSetCreated property while actual result code is {ResultCode}");
                    }
                    return _eventSetCreated;
                }
            }

            public EventSetWithEvents EventSetUpdated
            {
                get
                {
                    if (ResultCode != ResultCode.EventSetUpdated)
                    {
                        throw new InvalidOperationException(
                            $"Trying to get EventSetUpdated property while actual result code is {ResultCode}");
                    }
                    return _eventSetUpdated;
                }
            }

            public EventDetails EventSkipped
            {
                get
                {
                    if (ResultCode != ResultCode.EventSkipped)
                    {
                        throw new InvalidOperationException(
                            $"Trying to get EventSkipped property while actual result code is {ResultCode}");
                    }
                    return _eventSkipped;
                }
            }

            public EventDetails EventFailed
            {
                get
                {
                    if (ResultCode != ResultCode.EventFailed)
                    {
                        throw new InvalidOperationException(
                            $"Trying to get EventFailed property while actual result code is {ResultCode}");
                    }
                    return _eventFailed;
                }
            }

            public static Result CreateEventSetCreated(EventSetWithEvents eventsetWithEvents)
            {
                return new Result(ResultCode.EventSetCreated, eventsetWithEvents, null, null, null);
            }

            public static Result CreateEventSetUpdated(EventSetWithEvents eventsetWithEvents)
            {
                return new Result(ResultCode.EventSetUpdated, null, eventsetWithEvents, null, null);
            }

            public static Result CreateEventSkipped(EventDetails @event)
            {
                return new Result(ResultCode.EventSkipped, null, null, @event, null);
            }

            public static Result CreateEventFailed(EventDetails @event)
            {
                return new Result(ResultCode.EventFailed, null, null, null, @event);
            }

            public T Match<T>(Func<EventSetWithEvents, T> transformEventSetCreated,
                Func<EventSetWithEvents, T> transformEventSetUpdated,
                Func<EventDetails, T> transformEventSkipped,
                Func<EventDetails, T> transformEventFailed)
            {
                switch (ResultCode)
                {
                    case ResultCode.EventSetCreated:
                        return transformEventSetCreated(EventSetCreated);
                    case ResultCode.EventSetUpdated:
                        return transformEventSetUpdated(EventSetUpdated);
                    case ResultCode.EventSkipped:
                        return transformEventSkipped(EventSkipped);
                    case ResultCode.EventFailed:
                        return transformEventFailed(EventFailed);
                    default:
                        throw new InvalidOperationException("Invalid state");
                }
            }
        }

        public class TplDataflowFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver,
                    identityService, processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return new TplDataflowAsyncProcessor<EventDetails, Result>(ProcessEventDataflow);
            }

            private ISourceBlock<Result> ProcessEventDataflow(ISourceBlock<EventDetails> input)
            {
                return input
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafe(_logic.ProcessEventGroupsBatchSafe)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class TplDataflowAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver,
                    identityService, processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return new TplDataflowAsyncProcessor<EventDetails, Result>(ProcessEventDataflow);
            }

            private ISourceBlock<Result> ProcessEventDataflow(ISourceBlock<EventDetails> input)
            {
                return input
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafeAsync(_logic.ProcessEventGroupsBatchSafeAsync)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class ObservableFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create<EventDetails, Result>(ProcessEventDataflow);
            }

            private IObservable<Result> ProcessEventDataflow(IObservable<EventDetails> input)
            {
                return input
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafe(_logic.ProcessEventGroupsBatchSafe)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class ObservableAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create<EventDetails, Result>(ProcessEventDataflow);
            }

            private IObservable<Result> ProcessEventDataflow(IObservable<EventDetails> input)
            {
                return input
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafeAsync(_logic.ProcessEventGroupsBatchSafeAsync)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class EnumerableFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create((Func<IEnumerable<EventDetails>, IEnumerable<Result>>) ProcessEventDataflow);
            }

            private IEnumerable<Result> ProcessEventDataflow(IEnumerable<EventDetails> input)
            {
                return input
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafe(_logic.ProcessEventGroupsBatchSafe)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) => Logic.TransformResult(res));
            }
        }

        public class EnumerableAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create((Func<IEnumerable<EventDetails>, Task<IEnumerable<Result>>>) ProcessEventDataflowAsync);
            }

            private Task<IEnumerable<Result>> ProcessEventDataflowAsync(IEnumerable<EventDetails> input)
            {
                return input
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafeAsync(_logic.FilterSkippedEventGroup)
                    .BufferSafeAsync(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafeAsync(_logic.ProcessEventGroupsBatchSafeAsync)
                    .SelectManyAsync((Either<UnsuccessResult, SuccessResult> res) => Logic.TransformResult(res));
            }
        }

        public abstract class BaseDataflowFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return CreateDataflowAsyncProcessor(ProcessEventDataflow);
            }

            protected abstract IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc);

            private IDataflow<Result> ProcessEventDataflow(IDataflowFactory dataflowFactory, EventDetails @event)
            {
                return dataflowFactory.Return(@event)
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafe(_logic.ProcessEventGroupsBatchSafe)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public abstract class BaseDataflowAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return CreateDataflowAsyncProcessor(ProcessEventDataflow);
            }

            protected abstract IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc);

            private IDataflow<Result> ProcessEventDataflow(IDataflowFactory dataflowFactory, EventDetails @event)
            {
                return dataflowFactory.Return(@event)
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .BufferSafe(_configuration.EventGroupBatchTimeout, _configuration.EventGroupBatchSize)
                    .SelectManySafeAsync(_logic.ProcessEventGroupsBatchSafeAsync)
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class DataflowFactory : BaseDataflowFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new DataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class DataflowAsyncFactory : BaseDataflowAsyncFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new DataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class TplDataflowDataflowFactory : BaseDataflowFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new TplDataflowDataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class TplDataflowDataflowAsyncFactory : BaseDataflowAsyncFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new TplDataflowDataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class EnumerableOneByOneFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create((Func<IEnumerable<EventDetails>, IEnumerable<Result>>) ProcessEventDataflow);
            }

            private IEnumerable<Result> ProcessEventDataflow(IEnumerable<EventDetails> events)
            {
                return events
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup => 
                        innerGroup
                            .ToListEnumerable()
                            .SelectMany(_logic.ProcessEventGroupsSafe))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) => 
                        Logic.TransformResult(res));
            }
        }

        public class EnumerableOneByOneAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create((Func<IEnumerable<EventDetails>, Task<IEnumerable<Result>>>) ProcessEventDataflowAsync);
            }

            private Task<IEnumerable<Result>> ProcessEventDataflowAsync(IEnumerable<EventDetails> events)
            {
                return events
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafeAsync(_logic.FilterSkippedEventGroup)
                    .GroupBySafeAsync(group => group.EventSetType.GetCode())
                    .SelectManySafeAsync(innerGroup =>
                        innerGroup
                            .ToListEnumerable()
                            .SelectManyAsync(_logic.ProcessEventGroupsSafeAsync))
                    .SelectManyAsync((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class ObservableOneByOneFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create<EventDetails, Result>(ProcessEventDataflow);
            }

            private IObservable<Result> ProcessEventDataflow(IObservable<EventDetails> events)
            {
                return events
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup =>
                        innerGroup
                            .ToList()
                            .SelectMany(_logic.ProcessEventGroupsSafe))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class ObservableOneByOneAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return AsyncProcessor.Create<EventDetails, Result>(ProcessEventDataflow);
            }

            private IObservable<Result> ProcessEventDataflow(IObservable<EventDetails> events)
            {
                return events
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup =>
                        innerGroup
                            .ToList()
                            .SelectManyAsync(_logic.ProcessEventGroupsSafeAsync))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class TplDataflowOneByOneFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return new TplDataflowAsyncProcessor<EventDetails, Result>(ProcessEventDataflow);
            }

            private ISourceBlock<Result> ProcessEventDataflow(ISourceBlock<EventDetails> events)
            {
                return events
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup =>
                        innerGroup
                            .ToList()
                            .SelectMany(_logic.ProcessEventGroupsSafe))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class TplDataflowOneByOneAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return new TplDataflowAsyncProcessor<EventDetails, Result>(ProcessEventDataflow);
            }

            private ISourceBlock<Result> ProcessEventDataflow(ISourceBlock<EventDetails> events)
            {
                return events
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup =>
                        innerGroup
                            .ToList()
                            .SelectManyAsync(_logic.ProcessEventGroupsSafeAsync))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public abstract class BaseDataflowOneByOneFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return CreateDataflowAsyncProcessor(ProcessEventDataflow);
            }

            protected abstract IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc);

            private IDataflow<Result> ProcessEventDataflow(IDataflowFactory dataflowFactory, EventDetails @event)
            {
                return dataflowFactory.Return(@event)
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup => 
                        innerGroup
                            .ToList()
                            .SelectMany(_logic.ProcessEventGroupsSafe))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public abstract class BaseDataflowOneByOneAsyncFactory : IFactory
        {
            private Logic _logic;
            private IEventSetConfiguration _configuration;

            public IAsyncProcessor<EventDetails, Result> CreateStorageProcessor(
                Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService,
                IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration,
                Func<DateTime> currentTimeProvider)
            {
                _logic = new Logic(repositoryResolver, identityService,
                    processTypeManager, currentTimeProvider);
                _configuration = configuration;

                return CreateDataflowAsyncProcessor(ProcessEventDataflow);
            }

            protected abstract IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc);

            private IDataflow<Result> ProcessEventDataflow(IDataflowFactory dataflowFactory, EventDetails @event)
            {
                return dataflowFactory.Return(@event)
                    .Select(_logic.LogEvent)
                    .Buffer(_configuration.EventBatchTimeout, _configuration.EventBatchSize)
                    .SelectManyAsync(_logic.SplitEventsIntoGroupsSafeAsync)
                    .SelectSafe(_logic.FilterSkippedEventGroup)
                    .GroupBySafe(group => group.EventSetType.GetCode())
                    .SelectManySafe(innerGroup =>
                        innerGroup
                            .ToList()
                            .SelectManyAsync(_logic.ProcessEventGroupsSafeAsync))
                    .SelectMany((Either<UnsuccessResult, SuccessResult> res) =>
                        Logic.TransformResult(res));
            }
        }

        public class DataflowOneByOneFactory : BaseDataflowOneByOneFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new DataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class DataflowOneByOneAsyncFactory : BaseDataflowOneByOneAsyncFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new DataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class TplDataflowDataflowOneByOneFactory : BaseDataflowOneByOneFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new TplDataflowDataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        public class TplDataflowDataflowOneByOneAsyncFactory : BaseDataflowOneByOneAsyncFactory
        {
            protected override IAsyncProcessor<EventDetails, Result> CreateDataflowAsyncProcessor(Func<IDataflowFactory, EventDetails, IDataflow<Result>> bindFunc)
            {
                return new TplDataflowDataflowAsyncProcessor<EventDetails, Result>(bindFunc);
            }
        }

        private class SuccessResult
        {
            public SuccessResult(bool isCreated, EventSet eventSet, IList<EventDetails> events)
            {
                IsCreated = isCreated;
                EventSetWithEvents = new EventSetWithEvents { EventSet = eventSet, Events = events };
            }

            public bool IsCreated { get; }

            public EventSetWithEvents EventSetWithEvents { get; }
        }

        private class UnsuccessResult
        {
            private UnsuccessResult(bool isSkipped, IList<EventDetails> events, int errorCode, string errorMessage)
            {
                IsSkipped = isSkipped;
                Events = events;
                ErrorCode = errorCode;
                ErrorMessage = errorMessage;
            }

            public bool IsSkipped { get; }

            public IList<EventDetails> Events { get; }

            public int ErrorCode { get; }

            public string ErrorMessage { get; }

            public static UnsuccessResult CreateFailed(IList<EventDetails> events, int errorCode,
                string errorMessageFormat, params object[] args)
            {
                return new UnsuccessResult(false, events, errorCode, string.Format(errorMessageFormat, args));
            }

            public static UnsuccessResult CreateSkipped(IList<EventDetails> events)
            {
                return new UnsuccessResult(true, events, 0, string.Empty);
            }
        }

        private class EventGroup
        {
            public EventSetType EventSetType { get; set; }

            public EventSetProcessType EventSetProcessType { get; set; }

            public List<EventDetails> Events { get; set; }
        }

        private class Logic
        {
            private const string EventSetSequenceName = "EventSet";
            private readonly Func<DateTime> _currentTimeProvider;
            private readonly IIdentityManagementService _identityService;

            private readonly ILog _logger = LogManager.GetLogger(typeof(Logic));
            private readonly IEventSetProcessTypeManager _processTypeManager;

            private readonly Func<IEventSetRepository> _repositoryResolver;

            public Logic(Func<IEventSetRepository> repositoryResolver,
                IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager,
                Func<DateTime> currentTimeProvider)
            {
                _repositoryResolver = repositoryResolver;
                _identityService = identityService;
                _processTypeManager = processTypeManager;
                _currentTimeProvider = currentTimeProvider;
            }

            public EventDetails LogEvent(EventDetails @event)
            {
                _logger.DebugFormat("EventSet processing started for event [EventId = {0}]", @event.Id);
                return @event;
            }

            public IEnumerable<Either<UnsuccessResult, EventGroup>> SplitEventsIntoGroupsSafe(
                IList<EventDetails> events)
            {
                return events
                    .GroupBy(@event => new
                    {
                        EventTypeId = @event.EventTypeId,
                        EventCategory = @event.Category
                    })
                    .Select(
                        eventGroup =>
                            GetProcessTypeSafe(eventGroup.Key.EventTypeId, eventGroup.Key.EventCategory, eventGroup.ToList())
                                .Select(processType => new
                                {
                                    EventSetProcessType = processType,
                                    Events = eventGroup
                                }))
                    .SelectMany(processType => processType.Events,
                        (processType, @event) => new
                        {
                            Event = @event,
                            EventSetProcessType = processType.EventSetProcessType,
                            EventSetType = EventSetType.CreateFromEventAndLevel(@event,
                                    (EventLevel)processType.EventSetProcessType.Level)
                        })
                    .GroupBySafe(eventInfo => eventInfo.EventSetType)
                    .Select(eventGroup => new EventGroup
                    {
                        EventSetType = eventGroup.Key,
                        EventSetProcessType = eventGroup.First().EventSetProcessType,
                        Events = eventGroup.Select(arg => arg.Event).ToList()
                    })
                    .SelectMany(SplitEventGroupByThreshold);
            }

            public Task<IEnumerable<Either<UnsuccessResult, EventGroup>>> SplitEventsIntoGroupsSafeAsync(
                IList<EventDetails> events)
            {
                return events
                    .GroupBy(@event => new
                    {
                        EventTypeId = @event.EventTypeId,
                        EventCategory = @event.Category
                    })
                    .SelectAsync(
                        eventGroup =>
                            GetProcessTypeSafeAsync(eventGroup.Key.EventTypeId, eventGroup.Key.EventCategory, eventGroup.ToList())
                                .SelectAsync(processType => new
                                {
                                    EventSetProcessType = processType,
                                    Events = eventGroup
                                }))
                    .SelectManyAsync(processType => processType.Events,
                        (processType, @event) => new
                        {
                            Event = @event,
                            EventSetProcessType = processType.EventSetProcessType,
                            EventSetType = EventSetType.CreateFromEventAndLevel(@event,
                                    (EventLevel)processType.EventSetProcessType.Level)
                        })
                    .GroupBySafeAsync(eventInfo => eventInfo.EventSetType)
                    .SelectAsync(eventGroup => new EventGroup
                    {
                        EventSetType = eventGroup.Key,
                        EventSetProcessType = eventGroup.First().EventSetProcessType,
                        Events = eventGroup.Select(arg => arg.Event).ToList()
                    })
                    .SelectManyAsync(SplitEventGroupByThreshold);
            }

            public Either<UnsuccessResult, EventGroup> FilterSkippedEventGroup(EventGroup eventGroup)
            {
                if (NeedSkipEventGroup(eventGroup))
                {
                    return UnsuccessResult.CreateSkipped(eventGroup.Events);
                }
                return eventGroup;
            }

            public IEnumerable<Either<UnsuccessResult, SuccessResult>> ProcessEventGroupsBatchSafe(
                IList<EventGroup> eventGroupsBatch)
            {
                return EnumerableExtensions.Use(_repositoryResolver(), repository =>
                {
                    return FindLastEventSetsSafe(repository, eventGroupsBatch)
                        .SelectManySafe(
                            lastEventSets =>
                                InternalProcessEventGroupsBatchSafe(eventGroupsBatch, lastEventSets))
                        .BufferSafe(TimeSpan.MaxValue, int.MaxValue)
                        .SelectSafe(resultList => ApplyChangesSafe(repository, resultList))
                        .SelectMany(result => result);
                });
            }

            public Task<IEnumerable<Either<UnsuccessResult, SuccessResult>>> ProcessEventGroupsBatchSafeAsync(
                IList<EventGroup> eventGroupsBatch)
            {
                return EnumerableExtensions.UseAsync(_repositoryResolver(), repository => 
                    FindLastEventSetsSafeAsync(repository, eventGroupsBatch)
                        .SelectManySafeAsync(
                            lastEventSets =>
                                InternalProcessEventGroupsBatchSafeAsync(eventGroupsBatch, lastEventSets))
                        .BufferSafeAsync(TimeSpan.MaxValue, int.MaxValue)
                        .SelectSafeAsync(resultList => ApplyChangesSafeAsync(repository, resultList))
                        .SelectManyAsync(result => result));
            }

            public IEnumerable<Either<UnsuccessResult, SuccessResult>> ProcessEventGroupsSafe(IList<EventGroup> eventGroups)
            {
                return EnumerableExtensions.Use(_repositoryResolver(), repository =>
                {
                    return eventGroups
                        .Select(group =>
                            FindLastEventSetsSafe(repository, new[] { group })
                                .Select(lastEventSets => new { group, lastEventSets }))
                        .SelectSafe(item => InternalProcessEventGroupSafe(item.group, item.lastEventSets))
                        .SelectSafe(result => ApplyChangesSafe(repository, new[] { result }))
                        .SelectMany(result => result);
                });
            }

            public Task<IEnumerable<Either<UnsuccessResult, SuccessResult>>> ProcessEventGroupsSafeAsync(IList<EventGroup> eventGroups)
            {
                return EnumerableExtensions.UseAsync(_repositoryResolver(), repository => 
                    eventGroups
                        .SelectAsync(group => 
                            FindLastEventSetsSafeAsync(repository, new[] { group })
                                .SelectAsync(lastEventSets => new { group, lastEventSets }))
                        .SelectSafeAsync(item => InternalProcessEventGroupSafeAsync(item.group, item.lastEventSets))
                        .SelectSafeAsync(result => ApplyChangesSafeAsync(repository, new[] { result }))
                        .SelectManyAsync(result => result));
            }

            public static IEnumerable<Result> TransformResult(Either<UnsuccessResult, SuccessResult> result)
            {
                return result.Match(
                    successResult => successResult.IsCreated
                        ? List(Result.CreateEventSetCreated(successResult.EventSetWithEvents))
                        : List(Result.CreateEventSetUpdated(successResult.EventSetWithEvents)),
                    unsuccessResult => unsuccessResult.IsSkipped
                        ? unsuccessResult.Events.Select(Result.CreateEventSkipped)
                        : unsuccessResult.Events.Select(Result.CreateEventFailed));
            }

            private Either<UnsuccessResult, IList<EventSet>> FindLastEventSetsSafe(
                IEventSetRepository repository, IList<EventGroup> eventGroups)
            {
                var events = eventGroups
                    .SelectMany(group => group.Events)
                    .ToList();
                var typeCodes = eventGroups
                    .Select(group => group.EventSetType.GetCode())
                    .Distinct()
                    .ToList();
                return InvokeSafe(events, () => repository.FindLastEventSetsByTypeCodes(typeCodes));
            }

            private Task<Either<UnsuccessResult, IList<EventSet>>> FindLastEventSetsSafeAsync(
                IEventSetRepository repository, IList<EventGroup> eventGroups)
            {
                var events = eventGroups
                    .SelectMany(group => group.Events)
                    .ToList();
                var typeCodes = eventGroups
                    .Select(group => group.EventSetType.GetCode())
                    .Distinct()
                    .ToList();
                return InvokeSafeAsync(events, () => repository.FindLastEventSetsByTypeCodesAsync(typeCodes));
            }

            private Either<UnsuccessResult, SuccessResult> InternalProcessEventGroupSafe(EventGroup eventGroup, IList<EventSet> lastEventSets)
            {
                return NeedToCreateEventSet(eventGroup, lastEventSets)
                    ? CreateEventSetForEventGroup(eventGroup)
                    : UpdateEventSetForEventGroup(eventGroup, lastEventSets);
            }

            private Task<Either<UnsuccessResult, SuccessResult>> InternalProcessEventGroupSafeAsync(EventGroup eventGroup, IList<EventSet> lastEventSets)
            {
                return NeedToCreateEventSet(eventGroup, lastEventSets)
                    ? CreateEventSetForEventGroupAsync(eventGroup)
                    : UpdateEventSetForEventGroupAsync(eventGroup, lastEventSets);
            }

            private Either<UnsuccessResult, IList<SuccessResult>> ApplyChangesSafe(
                IEventSetRepository repository, IList<SuccessResult> results)
            {
                var events = results
                    .SelectMany(result => result.EventSetWithEvents.Events)
                    .ToList();
                return InvokeSafe(events, () => ApplyChanges(repository, results));
            }

            private Task<Either<UnsuccessResult, IList<SuccessResult>>> ApplyChangesSafeAsync(
                IEventSetRepository repository, IList<SuccessResult> results)
            {
                var events = results
                    .SelectMany(result => result.EventSetWithEvents.Events)
                    .ToList();
                return InvokeSafeAsync(events, async () => await ApplyChangesAsync(repository, results));
            }

            private static Either<UnsuccessResult, T> InvokeSafe<T>(
                IList<EventDetails> events, Func<Either<UnsuccessResult, T>> func)
            {
                try
                {
                    return func();
                }
                catch (EventHandlingException e)
                {
                    return UnsuccessResult.CreateFailed(events, e.ErrorCode, e.Message);
                }
                catch (DBConcurrencyException e)
                {
                    return UnsuccessResult.CreateFailed(events,
                            Metadata.ExceptionHandling.DbUpdateConcurrencyException.Code,
                            e.Message);
                }
                catch (Exception e)
                {
                    return UnsuccessResult.CreateFailed(events, Metadata.ExceptionHandling.UnhandledException.Code, e.Message);
                }
            }

            private static async Task<Either<UnsuccessResult, T>> InvokeSafeAsync<T>(
                IList<EventDetails> events, Func<Task<Either<UnsuccessResult, T>>> func)
            {
                try
                {
                    return await func();
                }
                catch (EventHandlingException e)
                {
                    return UnsuccessResult.CreateFailed(events, e.ErrorCode, e.Message);
                }
                catch (DBConcurrencyException e)
                {
                    return UnsuccessResult.CreateFailed(events,
                            Metadata.ExceptionHandling.DbUpdateConcurrencyException.Code,
                            e.Message);
                }
                catch (Exception e)
                {
                    return UnsuccessResult.CreateFailed(events, Metadata.ExceptionHandling.UnhandledException.Code, e.Message);
                }
            }

            private static Either<UnsuccessResult, T> InvokeSafe<T>(
                IList<EventDetails> events, Func<T> func)
            {
                return InvokeSafe(events, () => (Either<UnsuccessResult, T>)func());
            }

            private static Task<Either<UnsuccessResult, T>> InvokeSafeAsync<T>(
                IList<EventDetails> events, Func<Task<T>> func)
            {
                return InvokeSafeAsync(events, async () => (Either<UnsuccessResult, T>) await func());
            }

            private static bool NeedSkipEventGroup(EventGroup eventGroup)
            {
                return eventGroup.EventSetType.Level == EventLevel.Information;
            }

            private IEnumerable<Either<UnsuccessResult, SuccessResult>> InternalProcessEventGroupsBatchSafe(IList<EventGroup> eventGroupsBatch, IList<EventSet> lastEventSets)
            {
                return eventGroupsBatch
                    .GroupBy(eventGroup => NeedToCreateEventSet(eventGroup, lastEventSets))
                    .SelectMany(eventGroup => eventGroup.Key
                        ? CreateEventSetsForEventGroupBatch(eventGroup.ToList())
                        : UpdateEventSetsForEventGroupBatch(eventGroup.ToList(), lastEventSets));
            }

            private Task<IEnumerable<Either<UnsuccessResult, SuccessResult>>> InternalProcessEventGroupsBatchSafeAsync(IList<EventGroup> eventGroupsBatch, IList<EventSet> lastEventSets)
            {
                return eventGroupsBatch
                    .GroupBy(eventGroup => NeedToCreateEventSet(eventGroup, lastEventSets))
                    .SelectManyAsync(eventGroup => eventGroup.Key
                        ? CreateEventSetsForEventGroupBatchAsync(eventGroup.ToList())
                        : UpdateEventSetsForEventGroupBatchAsync(eventGroup.ToList(), lastEventSets));
            }

            private static IList<SuccessResult> ApplyChanges(IEventSetRepository repository, IList<SuccessResult> results)
            {
                repository.ApplyChanges(
                    results
                        .Where(result => result.IsCreated)
                        .Select(result => result.EventSetWithEvents.EventSet)
                        .ToList(),
                    results
                        .Where(result => !result.IsCreated)
                        .Select(result => result.EventSetWithEvents.EventSet)
                        .ToList());
                return results;
            }

            private static async Task<IList<SuccessResult>> ApplyChangesAsync(IEventSetRepository repository, IList<SuccessResult> results)
            {
                await repository.ApplyChangesAsync(
                    results
                        .Where(result => result.IsCreated)
                        .Select(result => result.EventSetWithEvents.EventSet)
                        .ToList(),
                    results
                        .Where(result => !result.IsCreated)
                        .Select(result => result.EventSetWithEvents.EventSet)
                        .ToList());
                return results;
            }

            private bool NeedToCreateEventSet(EventGroup eventGroup, IList<EventSet> lastEventSets)
            {
                EventSet lastEventSet =
                    lastEventSets.FirstOrDefault(set => set.TypeCode == eventGroup.EventSetType.GetCode());
                return lastEventSet == null
                       ||
                       IsEventSetOutdated(lastEventSet, eventGroup.EventSetProcessType,
                           eventGroup.Events.Min(@event => @event.ReadTime));
            }

            private bool IsEventSetOutdated(EventSet eventSet, EventSetProcessType eventSetProcessType,
                DateTime nextReadTime)
            {
                if (eventSet.Status == (int)EventSetStatus.Completed)
                {
                    _logger.DebugFormat("EventSet is already completed [Id = {0}].", eventSet.Id);
                    return true;
                }

                if (eventSetProcessType.Threshold.Ticks > 0 &&
                    nextReadTime - eventSet.LastReadTime >= eventSetProcessType.Threshold)
                {
                    _logger.DebugFormat("EventSet outdated by threshold [Id = {0}, Threshold = {1}].",
                        eventSet.Id, eventSetProcessType.Threshold);
                    return true;
                }

                if (eventSetProcessType.AutoComplete && eventSetProcessType.AutoCompleteTimeout.HasValue &&
                    ShouldBeCompletedByAutoComplete(eventSet, eventSetProcessType.AutoCompleteTimeout.Value))
                {
                    _logger.DebugFormat(
                        "EventSet should be completed [Id = {0}, Current Status = {1}, AutoCompleteTimeout = {2}].",
                        eventSet.Id, eventSet.Status, eventSetProcessType.AutoCompleteTimeout);
                    return true;
                }

                return false;
            }

            private bool ShouldBeCompletedByAutoComplete(EventSet eventSet, TimeSpan timeout)
            {
                DateTime autoCompleteTime = DateTime.UtcNow.Subtract(timeout);

                return (eventSet.Status == (byte)EventSetStatus.New && eventSet.CreationTime < autoCompleteTime)
                       ||
                       (
                           (eventSet.Status == (byte)EventSetStatus.Accepted ||
                            eventSet.Status == (byte)EventSetStatus.Rejected)
                           && eventSet.AcceptedTime < autoCompleteTime
                           );
            }

            private IEnumerable<Either<UnsuccessResult, SuccessResult>> CreateEventSetsForEventGroupBatch(IList<EventGroup> eventGroups)
            {
                var events = eventGroups
                    .SelectMany(group => group.Events)
                    .ToList();
                return GenerateEventSetIdsSafe(eventGroups.Count, events)
                    .SelectMany(eventSetIds =>
                        eventGroups
                            .Zip(eventSetIds, (eventGroup, eventSetId) =>
                                new { EventGroup = eventGroup, EventSetId = eventSetId })
                            .Select(item => CreateEventSet(item.EventSetId, item.EventGroup)));
            }

            private Task<IEnumerable<Either<UnsuccessResult, SuccessResult>>> CreateEventSetsForEventGroupBatchAsync(IList<EventGroup> eventGroups)
            {
                var events = eventGroups
                    .SelectMany(group => group.Events)
                    .ToList();
                return GenerateEventSetIdsSafeAsync(eventGroups.Count, events)
                    .SelectManyAsync(eventSetIds =>
                        eventGroups
                            .Zip(eventSetIds, (eventGroup, eventSetId) =>
                                new { EventGroup = eventGroup, EventSetId = eventSetId })
                            .Select(item => CreateEventSet(item.EventSetId, item.EventGroup)));
            }

            private Either<UnsuccessResult, SuccessResult> CreateEventSetForEventGroup(EventGroup eventGroup)
            {
                return GenerateEventSetIdsSafe(1, eventGroup.Events)
                    .Select(eventSetIds => CreateEventSet(eventSetIds.Single(), eventGroup));
            }

            private Task<Either<UnsuccessResult, SuccessResult>> CreateEventSetForEventGroupAsync(EventGroup eventGroup)
            {
                return GenerateEventSetIdsSafeAsync(1, eventGroup.Events)
                    .SelectAsync(eventSetIds => CreateEventSet(eventSetIds.Single(), eventGroup));
            }

            private Either<UnsuccessResult, IList<long>> GenerateEventSetIdsSafe(int amount, List<EventDetails> events)
            {
                return InvokeSafe(events, () => GenerateEventSetIds(amount));
            }

            private Task<Either<UnsuccessResult, IList<long>>> GenerateEventSetIdsSafeAsync(int amount, List<EventDetails> events)
            {
                return InvokeSafeAsync(events, async () => await GenerateEventSetIdsAsync(amount));
            }

            private IList<long> GenerateEventSetIds(int amount)
            {
                IList<long> ids = _identityService.GetNextLongIds(EventSetSequenceName, amount);

                if (ids.Count < amount)
                {
                    throw new InvalidOperationException(
                        "Not all event set identifiers are generated by identity service.");
                }

                return ids;
            }

            private async Task<IList<long>> GenerateEventSetIdsAsync(int amount)
            {
                IList<long> ids = await _identityService.GetNextLongIdsAsync(EventSetSequenceName, amount);

                if (ids.Count < amount)
                {
                    throw new InvalidOperationException(
                        "Not all event set identifiers are generated by identity service.");
                }

                return ids;
            }

            private SuccessResult CreateEventSet(long eventSetId, EventGroup eventGroup)
            {
                DateTime currentTime = _currentTimeProvider();
                var eventSet = new EventSet
                {
                    Id = eventSetId,
                    EventTypeId = eventGroup.EventSetType.EventTypeId,
                    EventTypeCategory = (byte)eventGroup.EventSetType.EventTypeCategory,
                    ResourceId = eventGroup.EventSetType.ResourceId,
                    ResourceCategory = eventGroup.EventSetType.ResourceCategory,
                    SiteId = eventGroup.EventSetType.SiteId,
                    Level = (byte)eventGroup.EventSetType.Level,
                    Status = (byte)EventSetStatus.New,
                    CreationTime = currentTime,
                    FirstReadTime = eventGroup.Events.Min(@event => @event.ReadTime),
                    LastReadTime = eventGroup.Events.Max(@event => @event.ReadTime),
                    LastUpdateTime = currentTime,
                    EventsCount = eventGroup.Events.Count,
                    TypeCode = eventGroup.EventSetType.GetCode()
                };

                _logger.DebugFormat(
                    "EventSet entity created [Id = {0}, EventTypeId = {1}, ResourceId = {2}, EventsCount = {3}, EventIds = {4}].",
                    eventSet.Id, eventSet.EventTypeId, eventSet.ResourceId, eventGroup.Events.Count,
                    string.Join(",", eventGroup.Events.Select(details => details.Id)));

                return new SuccessResult(isCreated: true, eventSet: eventSet, events: eventGroup.Events);
            }

            private IEnumerable<Either<UnsuccessResult, SuccessResult>> UpdateEventSetsForEventGroupBatch(IList<EventGroup> eventGroups,
                IList<EventSet> lastEventSets)
            {
                return eventGroups
                    .Select(eventGroup => UpdateEventSet(eventGroup, lastEventSets))
                    .Select(Right<UnsuccessResult, SuccessResult>);
            }

            private Task<IEnumerable<Either<UnsuccessResult, SuccessResult>>> UpdateEventSetsForEventGroupBatchAsync(IList<EventGroup> eventGroups,
                IList<EventSet> lastEventSets)
            {
                return eventGroups
                    .Select(eventGroup => UpdateEventSet(eventGroup, lastEventSets))
                    .Select(Right<UnsuccessResult, SuccessResult>)
                    .AsTask();
            }

            private Either<UnsuccessResult, SuccessResult> UpdateEventSetForEventGroup(EventGroup eventGroup,
                IList<EventSet> lastEventSets)
            {
                return Right<UnsuccessResult, SuccessResult>(
                    UpdateEventSet(eventGroup, lastEventSets));
            }

            private Task<Either<UnsuccessResult, SuccessResult>> UpdateEventSetForEventGroupAsync(EventGroup eventGroup,
                IList<EventSet> lastEventSets)
            {
                return Task.FromResult(Right<UnsuccessResult, SuccessResult>(
                    UpdateEventSet(eventGroup, lastEventSets)));
            }

            private SuccessResult UpdateEventSet(EventGroup eventGroup, IList<EventSet> lastEventSets)
            {
                IList<EventDetails> events = eventGroup.Events;
                EventSet lastEventSet = lastEventSets.First(set => set.TypeCode == eventGroup.EventSetType.GetCode());

                lastEventSet.FirstReadTime = events
                    .Select(@event => @event.ReadTime)
                    .Concat(new[] { lastEventSet.FirstReadTime })
                    .Min();
                lastEventSet.LastReadTime = events
                    .Select(@event => @event.ReadTime)
                    .Concat(new[] { lastEventSet.LastReadTime })
                    .Max();
                lastEventSet.LastUpdateTime = _currentTimeProvider();
                lastEventSet.EventsCount += events.Count;

                _logger.DebugFormat(
                    "EventSet entity updated [Id = {0}, EventTypeId = {1}, ResourceId = {2}, EventsCountDelta = {3}, EventsCount = {4}, EventIds = {5}].",
                    lastEventSet.Id, lastEventSet.EventTypeId, lastEventSet.ResourceId, events.Count,
                    lastEventSet.EventsCount, string.Join(",", events.Select(@event => @event.Id)));

                return new SuccessResult(isCreated: false, eventSet: lastEventSet, events: eventGroup.Events);
            }

            private IEnumerable<EventGroup> SplitEventGroupByThreshold(EventGroup eventGroup)
            {
                TimeSpan threshold = eventGroup.EventSetProcessType.Threshold;

                IEnumerator<EventDetails> enumerator = eventGroup.Events
                    .OrderBy(@event => @event.ReadTime)
                    .GetEnumerator();
                enumerator.MoveNext();

                DateTime lastTime = enumerator.Current.ReadTime;
                var events = new List<EventDetails>();

                do
                {
                    var newTime = enumerator.Current.ReadTime;

                    if (threshold.Ticks > 0 && newTime - lastTime >= threshold)
                    {
                        yield return new EventGroup
                        {
                            EventSetType = eventGroup.EventSetType,
                            EventSetProcessType = eventGroup.EventSetProcessType,
                            Events = events
                        };
                        events = new List<EventDetails>();
                    }

                    lastTime = newTime;
                    events.Add(enumerator.Current);
                } while (enumerator.MoveNext());

                yield return new EventGroup
                {
                    EventSetType = eventGroup.EventSetType,
                    EventSetProcessType = eventGroup.EventSetProcessType,
                    Events = events
                };
            }

            private Either<UnsuccessResult, EventSetProcessType> GetProcessTypeSafe(int eventTypeId,
                EventTypeCategory category, IList<EventDetails> events)
            {
                return InvokeSafe(events, () =>
                    _processTypeManager.GetProcessType(eventTypeId, category) ??
                        Left<UnsuccessResult, EventSetProcessType>(
                            UnsuccessResult.CreateFailed(events,
                                Metadata.ExceptionHandling.NotFoundException.Code,
                                "EventSetProcessingType was not found for [EventTypeId = {0}, Category = {1}]", eventTypeId,
                                category)));
            }

            private Task<Either<UnsuccessResult, EventSetProcessType>> GetProcessTypeSafeAsync(int eventTypeId,
                EventTypeCategory category, IList<EventDetails> events)
            {
                return InvokeSafeAsync(events, async () =>
                    await _processTypeManager.GetProcessTypeAsync(eventTypeId, category) ??
                        Left<UnsuccessResult, EventSetProcessType>(
                            UnsuccessResult.CreateFailed(events,
                                Metadata.ExceptionHandling.NotFoundException.Code,
                                "EventSetProcessingType was not found for [EventTypeId = {0}, Category = {1}]", eventTypeId,
                                category)));
            }
        }
    }
}