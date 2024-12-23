using System.Threading;
using System.Threading.Tasks;

namespace Code2.MessagingTests.Assets;

public class TestMessageHandler1 : IQueryHandler<TestMessage1, TestResponse1>, IMessageHandler<TestMessage2>
{
	public static TestMessageHandler1 Instance { get; private set; } = new();

	public async Task<TestResponse1> Handle(TestMessage1 message, CancellationToken cancellationToken = default)
	{
		return await Task.FromResult(new TestResponse1(message.Text));
	}

	public async Task Handle(TestMessage2 message, CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	public async Task Handle(TestMessage3 message, CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	public async Task Run(TestMessage1 message, CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	public async Task InvalidHandle(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}
