# Code2.Messaging
Minimal message bus 


## Configuration options

- LoadFromAssemblies, indicates whether to load handlers and event sources from assemblies
- LoadMessageHandlerFilter, handler type filter when loading from assemblies (optional)
- LoadEventSourceFilter, event source type filter when loading from assemblies (optional)
- MessageHandlerTypes, load handlers from an array of types (optional)
- EventSourceTypes, load event sources from an array of types (optional)
- MessageHandlerMethodName, message handlers method name (default: "Handle")
- EventSourceNamePrefix, event source property name prefix (default: "Publish")