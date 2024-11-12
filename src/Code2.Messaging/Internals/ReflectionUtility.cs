using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Messaging.Internals;

internal class ReflectionUtility : IReflectionUtility
{
	public ReflectionUtility()
	{
		_nonFrameworkAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !IsFrameworkAssembly(x)).ToArray();
		_nonFrameworkClasses = _nonFrameworkAssemblies.SelectMany(x => x.ExportedTypes).Where(x => x.IsClass).ToArray();
	}

	private readonly Assembly[] _nonFrameworkAssemblies;
	private readonly Type[] _nonFrameworkClasses;


	public Type[] GetNonFrameworkClasses(Func<Type, bool>? filter = null)
		=> _nonFrameworkClasses.Where(x => filter is null || filter(x)).ToArray();

	public (Delegate func, Type messageType, Type? taskResultType)[] GetMessageHandlerDelegates(object instance, string methodName)
	{
		Type type = instance.GetType();
		Type[] returnTypeFilter = new[] { typeof(Task), typeof(Task<>) };
		MethodInfo[] methods = type.GetMethods()
			.Where(x => x.Name == methodName && returnTypeFilter.Contains(x.ReturnType.IsGenericType ? x.ReturnType.GetGenericTypeDefinition() : x.ReturnType))
			.ToArray();
		return methods.Select(method =>
		{
			Type[] parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
			if (parameterTypes.Length != 2 || parameterTypes[1] != typeof(CancellationToken)) throw new InvalidOperationException($"Invalid message handler on type '{type}', expected [messageType, CancellationToken], found [{string.Join(",", parameterTypes.Select(x=>x.Name))}]");
			Type funcType = Expression.GetFuncType(parameterTypes[0], parameterTypes[1], method.ReturnType);
			Type? taskResultType = method.ReturnType.IsGenericType ? method.ReturnType.GenericTypeArguments.First() : null;
			Delegate func = Delegate.CreateDelegate(funcType, instance, method);
			return (func, parameterTypes[0], taskResultType);
		}).ToArray();
	}

	public string[] GetActionTypePropertyNames(Type type, string propertyNamePrefix, bool canWrite)
		=> type.GetProperties()
			.Where(x => x.Name.StartsWith(propertyNamePrefix) && x.CanWrite == canWrite && x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(Action<>))
			.Select(x => x.Name)
			.ToArray();

	public void SetPropertyValue(string propertyName, object instance, object? value)
	{
		var type = instance.GetType();
		var property = type.GetProperty(propertyName);
		if (property is null) throw new InvalidOperationException($"Property '{propertyName}' not found on type '{type}'");
		property.SetValue(instance, value);
	}

	public object? InvokePrivateGenericMethod(object instance, string methodName, Type[] genericArgumentTypes, object?[]? parameters)
	{
		MethodInfo? methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (methodInfo is null) throw new InvalidOperationException($"Method '{methodName}' not found");
		if (!methodInfo.IsGenericMethod) throw new InvalidOperationException("Method is not generic");

		return methodInfo.MakeGenericMethod(genericArgumentTypes).Invoke(instance, parameters);
	}

	public object ActivatorCreateInstance(IServiceProvider? serviceProvider, Type type)
	{
		if(serviceProvider is not null) return ActivatorUtilities.CreateInstance(serviceProvider, type);
		try
		{
			return Activator.CreateInstance(type)!;
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to create instance of type {type}", ex);
		}
	}

	private static bool IsFrameworkAssembly(Assembly assembly)
		=> assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product == "Microsoft® .NET";

}
