using Code2.Messaging;
using Code2.Messaging.Internals;
using Code2.MessagingTests.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Code2.MessagingTests;

[TestClass]
public class MessageBusTests
{

	private readonly MessageBusOptions _defaultOptions = MessageBus.GetDefaultOptions();

	[TestMethod]
	public void Configure_When_LoadFromAssemblies_Expect_HandlersLoaded()
	{
		var reflectionUtility = new ReflectionUtility();
		MessageBus messageBus = new(null, reflectionUtility);
		int expectedCount = GetMessageHandlersCount<TestMessageHandler1>(_defaultOptions.MessageHandlerMethodName);

		messageBus.Configure(x => x.LoadFromAssemblies = true);
		var handlers = messageBus.GetMessageHandlers();

		Assert.AreEqual(expectedCount, handlers.Length);
	}

	[TestMethod]
	public void Configure_When_LoadFromAssembliesWithFilter_Expect_HandlersLoadedAccoringToFilter()
	{
		var reflectionUtility = new ReflectionUtility();
		MessageBus messageBus = new(null, reflectionUtility);
		int expectedCount = GetMessageHandlersCount<TestMessageHandler1>(_defaultOptions.MessageHandlerMethodName);

		messageBus.Configure(x =>
		{
			x.LoadFromAssemblies = true;
			x.LoadTypeFilter = (t) => t.GetInterfaces().Where(x => x.IsGenericType).Select(x => x.GetGenericTypeDefinition()).Any(x => x == typeof(IQueryHandler<,>));
		});

		var handlers = messageBus.GetMessageHandlers();

		Assert.AreEqual(expectedCount, handlers.Length);
	}

	[TestMethod]
	public void Configure_When_LoadFromAssemblies_Expect_EventSourcesLoaded()
	{
		var reflectionUtility = new ReflectionUtility();
		MessageBus messageBus = new(null, reflectionUtility);
		int expectedCount = GetEventSourcesCount<TestEventSource>(_defaultOptions.EventSourceNamePrefix);

		messageBus.Configure(x => x.LoadFromAssemblies = true);
		var sources = messageBus.GetEventSources();

		Assert.AreEqual(expectedCount, sources.Length);
	}

	[TestMethod]
	public void Configure_When_LoadFromAssembliesWithFilter_Expect_EventSourcesLoadedAccordingToFilter()
	{
		var reflectionUtility = new ReflectionUtility();
		MessageBus messageBus = new(null, reflectionUtility);
		int expectedCount = GetEventSourcesCount<TestEventSource>(_defaultOptions.EventSourceNamePrefix);

		messageBus.Configure(x =>
		{
			x.LoadFromAssemblies = true;
			x.LoadTypeFilter = (t) => t.IsAssignableTo(typeof(IPublisher));
		});
		var sources = messageBus.GetEventSources();

		Assert.AreEqual(expectedCount, sources.Length);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public async Task SendAsync_When_QueryWithNoResponseHandler_Expect_Exception()
	{
		var reflectionUtility = new ReflectionUtility();
		MessageBus messageBus = new(null, reflectionUtility);
		messageBus.AddMessageHandlers(TestMessageHandler1.Instance);

		await messageBus.SendAsync<TestMessage2, TestResponse2>(new TestMessage2(1, string.Empty));
	}

	[TestMethod]
	public async Task AddEventSources_When_EventHasResponse_Expect_Response()
	{
		var reflectionUtility = new ReflectionUtility();
		MessageBus messageBus = new(null, reflectionUtility);
		messageBus.AddMessageHandlers(TestMessageHandler1.Instance);
		messageBus.AddEventSources(TestEventSource.Instance);
		string testText = "query1";

		var response = TestEventSource.Instance.RaiseQuery1(testText);

		Assert.IsNotNull(response);
		Assert.AreEqual(testText, response.Text);
	}


	private int GetEventSourcesCount<T>(string eventSourceNamePrefix)
		=> typeof(T).GetProperties().Where(x => x.CanWrite && x.Name.StartsWith(eventSourceNamePrefix)).Count();

	private int GetMessageHandlersCount<T>(string handlerMethodName)
		=> typeof(T).GetMethods().Where(x => x.Name == handlerMethodName).Count();

}