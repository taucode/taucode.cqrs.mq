using System;
using TauCode.Domain.Events;
using TauCode.Mq.Abstractions;

namespace TauCode.Cqrs.Mq
{
    public abstract class DomainEventConverterBase<TDomainEvent, TMessage> : IDomainEventConverter
        where TDomainEvent : class, IDomainEvent
        where TMessage : IMessage
    {
        protected abstract TMessage ConvertImpl(TDomainEvent typedDomainEvent);

        public IMessage Convert(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            if (domainEvent is TDomainEvent typedDomainEvent)
            {
                return this.ConvertImpl(typedDomainEvent);
            }

            throw new ArgumentException(
                $"'{nameof(domainEvent)}' was expected to be of type '{typeof(TDomainEvent).FullName}', but appeared to be of type '{domainEvent.GetType().FullName}'.");

        }
    }
}
