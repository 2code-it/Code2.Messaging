using Code2.Messaging;
using Code2.Messaging.Internals;
using Code2.MessagingTests.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.MessagingTests;

[TestClass]
public class ReflectionUtiltiyTests
{
	[TestMethod]
	public void GetNonFrameworkClasses_When_Called_Expect_NonFrameworkClasses()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();

		var classes = reflectionUtility.GetNonFrameworkClasses();
		string?[] assemblyProductList = classes.Select(x => x.Assembly).Distinct().Select(x => x.GetCustomAttribute<AssemblyProductAttribute>()?.Product).ToArray();

		Assert.IsFalse(assemblyProductList.Contains("Microsoft® .NET"));
	}

	[TestMethod]
	[DataRow("Handle", 3)]
	[DataRow("Run", 1)]
	[DataRow("Unknown", 0)]
	public void GetMethodDelegates_When_MethodNameFilter_Expect_DelegatePerMatchedMethod(string methodName, int expectedCount)
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		var target = new TestMessageHandler1();

		var delegates = reflectionUtility.GetMessageHandlerDelegates(target, methodName);

		Assert.AreEqual(expectedCount, delegates.Length);
	}

	[TestMethod]
	public void GetMethodDelegates_When_FuncAccordingToDelegate_Expect_NoException()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		var target = new TestMessageHandler1();
		Type taskType = typeof(Task<>);

		var delegates = reflectionUtility.GetMessageHandlerDelegates(target, "Handle");
		var func = (Func<TestMessage1, CancellationToken, Task<TestResponse1>>)delegates[0].func;
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void GetMethodDelegates_When_IncompatibleDelegate_Expect_Exception()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		var target = new TestMessageHandler1();

		var delegates = reflectionUtility.GetMessageHandlerDelegates(target, "InvalidHandle");
	}

	[TestMethod]
	public void GetMethodDelegates_When_DelegateIsAction_Expect_RetrunTypeIsNull()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		var target = new TestMessageHandler1();

		var delegates = reflectionUtility.GetMessageHandlerDelegates(target, "Run");
		var returnType = delegates[0].taskResultType;

		Assert.IsNull(returnType);
	}

	[TestMethod]
	public void GetMethodDelegates_When_DelegateIsFunc_Expect_ReturnTypeIsNotNull()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		var target = new TestMessageHandler1();

		var delegates = reflectionUtility.GetMessageHandlerDelegates(target, "Handle");
		var returnType = delegates[0].taskResultType;

		Assert.IsNotNull(returnType);
	}

	[TestMethod]
	[DataRow(typeof(TestEventSource), "Publish", 2)]
	[DataRow(typeof(TestEventSource), "Unknown", 0)]
	public void GetPropertyNames_When_Called_Expect_PropertyNamesAccordingToArguments(Type type, string propertyNamePrefix, int expectedAmount)
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		bool propertyTypeFilter(Type type) => type.IsGenericType && (
			(type.GetGenericTypeDefinition() == typeof(Func<,,>) && type.GenericTypeArguments[1] == typeof(CancellationToken) && type.GenericTypeArguments[2] == typeof(Task))
			|| type.GetGenericTypeDefinition() == typeof(Action<>));

		string[] propertyNames = reflectionUtility.GetPropertyNames(type, x => x.CanWrite && x.Name.StartsWith(propertyNamePrefix), propertyTypeFilter);

		Assert.AreEqual(expectedAmount, propertyNames.Length);
	}

	[TestMethod]
	public void SetPropertyValue_When_DelegateTypeMatch_Expect_InvokeSuccess()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		TestEventSource eventSource = new TestEventSource();
		string textSend = "new message";
		string textReceive = string.Empty;
		Func<TestMessage1, CancellationToken, Task> delegate1 = (TestMessage1 message, CancellationToken token) => { textReceive = message.Text; return Task.CompletedTask; };

		reflectionUtility.SetPropertyValue(nameof(TestEventSource.PublishEvent1), eventSource, delegate1);
		eventSource.PublishEvent1?.Invoke(new TestMessage1(textSend), default);

		Assert.AreEqual(textSend, textReceive);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void SetPropertyValue_When_DelegateTypeNotMatch_Expect_Exception()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		TestEventSource eventSource = new TestEventSource();
		Action<TestMessage1, int> delegate1 = (TestMessage1 message, int id) => { };

		reflectionUtility.SetPropertyValue(nameof(TestEventSource.PublishEvent1), eventSource, delegate1);
	}

	[TestMethod]
	public void InvokePrivateGenericMethod_When_Invoke_Expect_Result()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();

		string? result = (string?)reflectionUtility.InvokePrivateGenericMethod(this, nameof(GetTypeName), new[] { typeof(TestMessage1) }, new object[] { new TestMessage1("") });

		Assert.AreEqual(result, typeof(TestMessage1).Name);
	}

	[TestMethod]
	public void GetOrCreateInstance_When_TargetHasNoDependency_Expect_CreateSuccess()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();

		CreateInstanceSubject subject = (CreateInstanceSubject)reflectionUtility.GetOrCreateInstance(null, typeof(CreateInstanceSubject));

		Assert.IsNotNull(subject);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void GetOrCreateInstance_When_NoServiceProviderAndTargetHasDependency_Expect_Exception()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();

		CreateInstanceSubject subject = (CreateInstanceSubject)reflectionUtility.GetOrCreateInstance(null, typeof(CreateInstanceSubjectWithDependency));
	}

	//
	private class CreateInstanceSubject { };
	private class CreateInstanceSubjectWithDependency
	{
		public CreateInstanceSubjectWithDependency(IMessageBus MessageBus) { }
	}

	private string GetTypeName<T>(T instance) where T : class
	{
		return typeof(T).Name;
	}
}
