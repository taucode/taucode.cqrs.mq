using System;
using System.Collections.Generic;
using System.Linq;
using TauCode.Domain.Events;
using TauCode.Mq.Abstractions;

namespace TauCode.Cqrs.Mq
{
    public class MultiDomainEventConverter : IDomainEventConverter
    {
        private readonly Dictionary<Type, IDomainEventConverter> _nestedConverters;

        public MultiDomainEventConverter(IEnumerable<IDomainEventConverter> nestedConverters)
        {
            if (nestedConverters == null)
            {
                throw new ArgumentNullException(nameof(nestedConverters));
            }

            _nestedConverters = new Dictionary<Type, IDomainEventConverter>();

            foreach (var nestedConverter in nestedConverters)
            {
                this.AddNestedConverter(nestedConverter);
            }
        }

        public void AddNestedConverter(IDomainEventConverter nestedConverter)
        {
            if (nestedConverter == null)
            {
                throw new ArgumentNullException(nameof(nestedConverter));
            }

            try
            {
                var baseType =
                    nestedConverter.GetType().BaseType ??
                    throw new ArgumentException("Base type is null.", nameof(nestedConverter));

                var isConstructedGenericType = baseType.IsConstructedGenericType;
                if (!isConstructedGenericType)
                {
                    throw new ArgumentException("Base type is not a constructed generic type.", nameof(nestedConverter));
                }

                var genericTypeDefinition = baseType.GetGenericTypeDefinition();
                if (genericTypeDefinition != typeof(DomainEventConverterBase<,>))
                {
                    throw new ArgumentException("Wrong base type's generic type definition", nameof(nestedConverter));
                }

                var domainEventType = baseType.GetGenericArguments().First();
                if (_nestedConverters.ContainsKey(domainEventType))
                {
                    throw new InvalidOperationException($"There is already a converter registered for the domain type '{domainEventType.FullName}'.");
                }

                _nestedConverters.Add(domainEventType, nestedConverter);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("Nested converter must be inherited from 'DomainEventConverterBase<YourDomainEvent, YourMessage>'.", nameof(nestedConverter), ex);
            }
        }

        public IMessage Convert(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            var domainEventType = domainEvent.GetType();
            _nestedConverters.TryGetValue(domainEventType, out var converter);
            if (converter == null)
            {
                throw new InvalidOperationException($"There is no converter for domain event type '{domainEventType.FullName}' registered.");
            }

            var message = converter.Convert(domainEvent);
            return message;
        }
    }
}
