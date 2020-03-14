using TauCode.Domain.Events;
using TauCode.Mq.Abstractions;

namespace TauCode.Cqrs.Mq
{
    public interface IDomainEventConverter
    {
        IMessage Convert(IDomainEvent domainEvent);
    }
}
