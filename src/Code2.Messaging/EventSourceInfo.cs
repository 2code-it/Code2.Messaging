using System;

namespace Code2.Messaging;

public class EventSourceInfo
{
	public EventSourceInfo(object instance, string propertyName, Type messageType)
	{
		Instance = instance;
		PropertyName = propertyName;
		MessageType = messageType;
	}

	public object Instance { get; private set; }
	public string PropertyName { get; private set; }
	public Type MessageType { get; private set; }
}


