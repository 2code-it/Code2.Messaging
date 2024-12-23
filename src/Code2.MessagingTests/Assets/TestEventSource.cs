using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.MessagingTests.Assets;

public class TestEventSource : IPublisher
{

	public static TestEventSource Instance { get; private set; } = new();

	public Func<TestMessage1, CancellationToken, Task<TestResponse1>>? PublishQuery1 { get; set; }
	public Func<TestMessage1, CancellationToken, Task>? PublishEvent1 { get; set; }
	public Action<TestMessage2>? PublishEvent2 { get; set; }

	public TestResponse1? RaiseQuery1(string messageText)
	{
		return PublishQuery1?.Invoke(new TestMessage1(messageText), default).Result;
	}

	public void RaiseEvent1(string messageText)
	{
		PublishEvent1?.Invoke(new TestMessage1(messageText), default).RunSynchronously();
	}

	public void RaiseEvent2(string messageText)
	{
		PublishEvent2?.Invoke(new TestMessage2(1, messageText));
	}
}
