using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TauCode.Cqrs.Commands;
using TauCode.Domain.Events;
using TauCode.Mq;

namespace TauCode.Cqrs.Mq
{
    public abstract class DomainEventAwareCommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
    {
        private class AllEventCatcher : IDomainEventSubscriber<IDomainEvent>
        {
            private readonly DomainEventAwareCommandHandler<TCommand> _owner;

            public AllEventCatcher(DomainEventAwareCommandHandler<TCommand> owner)
            {
                _owner = owner;
            }

            public void HandleEvent(IDomainEvent domainEvent)
            {
                try
                {
                    if (domainEvent == null)
                    {
                        throw new ArgumentNullException(nameof(domainEvent));
                    }

                    var message = _owner.DomainEventConverter.Convert(domainEvent);
                    var topic = _owner.GetTopic(domainEvent);

                    if (topic == null)
                    {
                        _owner.MessagePublisher.Publish(message);
                    }
                    else
                    {
                        _owner.MessagePublisher.Publish(message, topic);
                    }

                    

                    //var message = _domainEventConverter.Convert(domainEvent);
                    //_messagePublisher.Publish(message);

                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error occured while handling domain event.");
                }
            }
        }

        protected DomainEventAwareCommandHandler(
            IMessagePublisher messagePublisher,
            IDomainEventConverter domainEventConverter)
        {
            this.MessagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            this.DomainEventConverter =
                domainEventConverter ?? throw new ArgumentNullException(nameof(domainEventConverter));
        }

        protected IMessagePublisher MessagePublisher { get; private set; }
        protected IDomainEventConverter DomainEventConverter { get; private set; }

        protected abstract void ExecuteImpl(TCommand command);
        protected abstract Task ExecuteAsyncImpl(TCommand command, CancellationToken cancellationToken = default);

        protected virtual string GetTopic(IDomainEvent domainEvent) => null;

        public void Execute(TCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var catcher = new AllEventCatcher(/*this.MessagePublisher, this.DomainEventConverter*/ this);
            DomainEventPublisher.Current.Subscribe(catcher);

            try
            {
                this.ExecuteImpl(command);
            }
            finally
            {
                DomainEventPublisher.Current.Unsubscribe(catcher);
            }
        }

        public async Task ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var catcher = new AllEventCatcher(/*this.MessagePublisher, this.DomainEventConverter*/ this);
            DomainEventPublisher.Current.Subscribe(catcher);

            try
            {
                await this.ExecuteAsyncImpl(command, cancellationToken);
            }
            finally
            {
                DomainEventPublisher.Current.Unsubscribe(catcher);
            }
        }
    }
}
