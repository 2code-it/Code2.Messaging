using System;

namespace Code2.Messaging;

public class MessageHandlerInfo
{
	public MessageHandlerInfo(Delegate handler, object instance, Type messageType, Type? resultTaskGenericArgument)
	{
		Handler = handler;
		Instance = instance;
		MessageType = messageType;
		ResultTaskGenericArgument = resultTaskGenericArgument;
	}

	public Delegate Handler { get; private set; }
	public object Instance { get; private set; }
	public Type MessageType { get; private set; }
	public Type? ResultTaskGenericArgument { get; private set; }
}
