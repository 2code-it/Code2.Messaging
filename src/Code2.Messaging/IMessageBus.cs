using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Messaging;

public interface IMessageBus
{
	Task SendAsync<M>(M message, CancellationToken cancellationToken = default) where M : notnull;
	Task<R> SendAsync<M, R>(M message, CancellationToken cancellationToken = default) where M : notnull;

	void Configure(Action<MessageBusOptions> config);

	MessageHandlerInfo[] GetMessageHandlers(Type? messageType = null, Type? returnType = null, Type? instanceType = null, object? instance = null);
	EventSourceInfo[] GetEventSources(Type? messageType = null, Type? instanceType = null, object? instance = null);

	int Add(params object[] instance);
	int Remove(params object[] instance);
}
