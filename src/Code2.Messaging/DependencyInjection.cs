using Microsoft.Extensions.DependencyInjection;
using System;

namespace Code2.Messaging;

public static class DependencyInjection
{
	public static IServiceCollection AddMessageBus(this IServiceCollection services)
	{
		services.AddSingleton<IMessageBus, MessageBus>();
		return services;
	}

	public static IServiceProvider UseMessageBus(this IServiceProvider serviceProvider, Action<MessageBusOptions>? config = null)
	{
		IMessageBus messageBus = serviceProvider.GetRequiredService<IMessageBus>();
		if(config is not null) messageBus.Configure(config);
		return serviceProvider;
	}
}
