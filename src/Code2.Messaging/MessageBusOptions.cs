using System;

namespace Code2.Messaging;

public class MessageBusOptions
{
	public bool LoadFromAssemblies { get; set; }
	public Func<Type, bool>? LoadTypeFilter { get; set; }
	public Type[]? MessageHandlerTypes { get; set; }
	public Type[]? EventSourceTypes { get; set; }
	public string EventSourceNamePrefix { get; set; } = "Publish";
	public string MessageHandlerMethodName { get; set; } = "Handle";
}
