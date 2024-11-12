
using System;

namespace Code2.Messaging.Internals;

internal interface IReflectionUtility
{
	Type[] GetNonFrameworkClasses(Func<Type, bool>? filter = null);
	(Delegate func, Type messageType, Type? taskResultType)[] GetMessageHandlerDelegates(object instance, string methodName);
	string[] GetActionTypePropertyNames(Type type, string propertyNamePrefix, bool canWrite);
	void SetPropertyValue(string propertyName, object instance, object? value);
	object? InvokePrivateGenericMethod(object instance, string methodName, Type[] genericArgumentTypes, object?[]? parameters);
	object ActivatorCreateInstance(IServiceProvider? serviceProvider, Type type);
}