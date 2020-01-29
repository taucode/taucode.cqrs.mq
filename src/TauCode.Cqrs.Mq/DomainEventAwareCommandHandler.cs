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
            private readonly IMessagePublisher _messagePublisher;
            private readonly IDomainEventConverter _domainEventConverter;

            public AllEventCatcher(IMessagePublisher messagePublisher, IDomainEventConverter domainEventConverter)
            {
                _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
                _domainEventConverter =
                    domainEventConverter ?? throw new ArgumentNullException(nameof(domainEventConverter));
            }

            public void HandleEvent(IDomainEvent domainEvent)
            {
                try
                {
                    if (domainEvent == null)
                    {
                        throw new ArgumentNullException(nameof(domainEvent));
                    }

                    var message = _domainEventConverter.Convert(domainEvent);
                    _messagePublisher.Publish(message);

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

        public void Execute(TCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var catcher = new AllEventCatcher(this.MessagePublisher, this.DomainEventConverter);
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

            var catcher = new AllEventCatcher(this.MessagePublisher, this.DomainEventConverter);
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
