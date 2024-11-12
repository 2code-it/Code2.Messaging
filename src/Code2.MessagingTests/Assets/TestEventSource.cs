﻿using System;
using System.Threading.Tasks;

namespace Code2.MessagingTests.Assets;

public class TestEventSource: IPublisher
{

	public static TestEventSource Instance { get; private set; } = new();

	public Action<TestMessage1>? PublishEvent1 { get; set; }
	public Action<TestMessage2>? PublishEvent2 { get; set; }

	public void RaiseEvent1(string messageText)
	{
		PublishEvent1?.Invoke(new TestMessage1(messageText));
	}

	public void RaiseEvent2(string messageText)
	{
		PublishEvent2?.Invoke(new TestMessage2(1, messageText));
	}
}