using System;

namespace Code2.MessagingTests.Assets;

public record TestMessage1(string Text);
public record TestMessage2(int Id, string Text);
public record TestMessage3(string Text, DateTime Created);

public record TestResponse1(string Text);
public record TestResponse2(int Id, string Text);
