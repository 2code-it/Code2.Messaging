
using Code2.Messaging.Internals;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Messaging;

public class MessageBus : IMessageBus
{
	public MessageBus() : this(null, new ReflectionUtility())
	{ }

	public MessageBus(IServiceProvider serviceProvider) : this(serviceProvider, new ReflectionUtility())
	{ }

	internal MessageBus(IServiceProvider? serviceProvider, IReflectionUtility reflectionUtility)
	{
		_reflectionUtility = reflectionUtility;
		_serviceProvider = serviceProvider;
	}

	private readonly IReflectionUtility _reflectionUtility;
	private readonly IServiceProvider? _serviceProvider;

	private readonly MessageBusOptions _options = GetDefaultOptions();
	private readonly Dictionary<Type, List<MessageHandlerInfo>> _messageHandlers = new();
	private readonly List<EventSourceInfo> _eventSources = new();


	public async Task SendAsync<M>(M message, CancellationToken cancellationToken = default) where M : notnull
	{
		if (!_messageHandlers.ContainsKey(typeof(M))) return;
		List<MessageHandlerInfo> messageHandlers = _messageHandlers[typeof(M)];
		var tasks = messageHandlers.Where(x => x.ResultTaskGenericArgument is null)
			.AsParallel()
			.Select(x => TryInvokeHandler<M, Task>(message, x.Handler, cancellationToken))
			.ToArray();

		await Task.WhenAll(tasks);
	}

	public async Task<R> SendAsync<M, R>(M message, CancellationToken cancellationToken = default) where M : notnull
	{
		MessageHandlerInfo? messageHandler = _messageHandlers.ContainsKey(typeof(M))
			? _messageHandlers[typeof(M)].Where(x => x.ResultTaskGenericArgument == typeof(R)).FirstOrDefault()
			: null;
		if (messageHandler is null) throw new InvalidOperationException($"Handler not found for message '{typeof(M)}' and response '{typeof(R)}'");

		return await TryInvokeHandler<M, Task<R>>(message, messageHandler.Handler, cancellationToken);
	}

	private T TryInvokeHandler<M, T>(M message, Delegate handler, CancellationToken cancellationToken) where T: Task
	{
		Func<M, CancellationToken, T> func = (Func<M, CancellationToken, T>)handler;
		try
		{
			return func(message, cancellationToken);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to invoke handler for message '{typeof(M)}' on type '{handler.Method.DeclaringType}'", ex);
		}
	}

	public void Configure(Action<MessageBusOptions> config)
	{
		config(_options);
		RemoveMessageHandlers();
		RemoveEventSources();

		Type[]? handlerTypes = true switch
		{
			true when _options.LoadFromAssemblies => _reflectionUtility.GetNonFrameworkClasses(MessageHandlerTypeFilter),
			true when _options.MessageHandlerTypes is not null => _options.MessageHandlerTypes,
			_ => null
		};
		if (handlerTypes is not null) AddMessageHandlers(handlerTypes);


		Type[]? eventSourceTypes = true switch
		{
			true when _options.LoadFromAssemblies => _reflectionUtility.GetNonFrameworkClasses(EventSourceTypeFilter),
			true when _options.EventSourceTypes is not null => _options.EventSourceTypes,
			_ => null
		};
		if (eventSourceTypes is not null) AddEventSources(eventSourceTypes);
	}

	public int Add(params object[] instances)
		=> instances.Select(x => AddMessageHandlers(x) + AddEventSources(x)).Sum();

	public int Remove(params object[] instances)
		=> instances.Select(x => RemoveMessageHandlers(instance: x) + RemoveEventSources(instance: x)).Sum();

	public int AddMessageHandlers(IEnumerable<Type> types)
		=> types.Select(AddMessageHandlers).ToArray().Sum();

	public int AddMessageHandlers(Type type)
	{
		var existing = GetHandlerOrEventSourceInstance(type);
		object instance = existing is not null ? existing : _serviceProvider?.GetService(type) ?? _reflectionUtility.ActivatorCreateInstance(_serviceProvider, type);
		return AddMessageHandlers(instance);
	}

	public int AddMessageHandlers(object instance)
	{
		var delegateInfos = _reflectionUtility.GetMessageHandlerDelegates(instance, _options.MessageHandlerMethodName);
		MessageHandlerInfo[] handlers = delegateInfos.Select(x => new MessageHandlerInfo(x.func, instance, x.messageType, x.taskResultType)).ToArray();
		foreach (var handler in handlers)
		{
			if (!_messageHandlers.ContainsKey(handler.MessageType)) _messageHandlers.Add(handler.MessageType, new List<MessageHandlerInfo>());
			_messageHandlers[handler.MessageType].Add(handler);
		}
		return handlers.Count();
	}

	public int RemoveMessageHandlers(Type? messageType = null, Type? returnType = null, Type? instanceType = null, object? instance = null)
	{
		var handlers = GetMessageHandlers(messageType, returnType, instanceType, instance);
		foreach (var handler in handlers)
		{
			RemoveMessageHandler(handler);
		}
		return handlers.Length;
	}

	public void RemoveMessageHandler(MessageHandlerInfo messageHandler)
	{
		if (!_messageHandlers.ContainsKey(messageHandler.MessageType)) return;
		_messageHandlers[messageHandler.MessageType].Remove(messageHandler);
		if (_messageHandlers[messageHandler.MessageType].Count == 0) _messageHandlers.Remove(messageHandler.MessageType);
	}

	public MessageHandlerInfo[] GetMessageHandlers(Type? messageType = null, Type? taskType = null, Type? instanceType = null, object? instance = null)
		=> _messageHandlers.Values.SelectMany(x => x).Where(x =>
			(messageType is null || x.MessageType == messageType)
			&& (taskType is null || x.ResultTaskGenericArgument == taskType)
			&& (instanceType is null || x.Instance?.GetType() == instanceType)
			&& (instance is null || x.Instance == instance)
		).ToArray();

	public int AddEventSources(IEnumerable<Type> types)
		=> types.Select(AddEventSources).ToArray().Sum();

	public int AddEventSources(Type type)
	{
		var existing = GetHandlerOrEventSourceInstance(type);
		object instance = existing is not null? existing: _serviceProvider?.GetService(type) ?? _reflectionUtility.ActivatorCreateInstance(_serviceProvider, type);
		return AddEventSources(instance);
	}

	public int AddEventSources(object instance)
	{
		Type type = instance.GetType();
		string[] propertyNames = _reflectionUtility.GetActionTypePropertyNames(type, _options.EventSourceNamePrefix, true);
		foreach (string propertyName in propertyNames)
		{
			Type messageType = type.GetProperty(propertyName)!.PropertyType.GetGenericArguments()[0];
			object action = _reflectionUtility.InvokePrivateGenericMethod(this, nameof(GetPublishAction), new[] { messageType }, null)!;
			_reflectionUtility.SetPropertyValue(propertyName, instance, action);
			_eventSources.Add(new EventSourceInfo(instance, propertyName, messageType));
		}

		return propertyNames.Length;
	}

	public int RemoveEventSources(Type? messageType = null, Type? instanceType = null, object? instance = null)
	{
		var eventSources = GetEventSources(messageType, instanceType, instance);
		foreach (var eventSource in eventSources)
		{
			RemoveEventSource(eventSource);
		}
		return eventSources.Length;
	}

	public void RemoveEventSource(EventSourceInfo eventSource)
	{
		_reflectionUtility.SetPropertyValue(eventSource.PropertyName, eventSource.Instance, null);
		_eventSources.Remove(eventSource);
	}

	public EventSourceInfo[] GetEventSources(Type? messageType = null, Type? instanceType = null, object? instance = null)
		=> _eventSources.Where(x =>
				(messageType is null || x.MessageType == messageType)
				&& (instanceType is null || x.Instance.GetType() == instanceType)
				&& (instance is null || x.Instance == instance)
			).ToArray();

	public static MessageBusOptions GetDefaultOptions()
		=> new();


	private object? GetHandlerOrEventSourceInstance(Type handlerOrEventSourceType)
	{
		object? instance = _messageHandlers.Values.Select(x => x.FirstOrDefault(y=>y.Instance.GetType() == handlerOrEventSourceType)).FirstOrDefault();
		if (instance is not null) return instance;
		return _eventSources.FirstOrDefault(x => x.Instance.GetType() == handlerOrEventSourceType)?.Instance;
	}

	private bool MessageHandlerTypeFilter(Type type)
		=> type.GetMethods().Any(x =>
			x.Name == _options.MessageHandlerMethodName)
			&& (_options.LoadMessageHandlerFilter is null || _options.LoadMessageHandlerFilter(type));

	private bool EventSourceTypeFilter(Type type)
		=> type.GetProperties().Any(x =>
			x.Name.StartsWith(_options.EventSourceNamePrefix)
			&& x.PropertyType.IsGenericType
			&& x.PropertyType.GetGenericTypeDefinition() == typeof(Action<>))
			&& (_options.LoadEventSourceFilter is null || _options.LoadEventSourceFilter(type));

	private Action<T> GetPublishAction<T>() where T : class
		=> async (T message) => { await SendAsync(message); };
}