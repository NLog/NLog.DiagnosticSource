# NLog.DiagnosticSource
NLog ActivityTraceLayoutRenderer for [Microsoft Activity Trace](https://github.com/dotnet/runtime/blob/master/src/libraries/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md)

NLog DiagnosticListenerTarget for [Microsoft DiagnosticSource](https://github.com/dotnet/runtime/blob/master/src/libraries/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)

[![Version](https://badge.fury.io/nu/NLog.DiagnosticSource.svg)](https://www.nuget.org/packages/NLog.DiagnosticSource)
[![AppVeyor](https://img.shields.io/appveyor/ci/nlog/NLog-DiagnosticSource/master.svg)](https://ci.appveyor.com/project/nlog/NLog-DiagnosticSource/branch/master)

### How to install

1) Install the package

    `Install-Package NLog.DiagnosticSource` or in your csproj:

    ```xml
    <PackageReference Include="NLog.DiagnosticSource" Version="6.*" />
    ```

2) Add to your nlog.config:

    ```xml
    <extensions>
        <add assembly="NLog.DiagnosticSource"/>
    </extensions>
    ```

   Alternative register from code using fluent configuration API:
   ```csharp
   LogManager.Setup().SetupExtensions(ext => {
      ext.RegisterTarget<NLog.Targets.DiagnosticListenerTarget>();
      ext.RegisterLayoutRenderer<NLog.LayoutRenderers.ActivityTraceLayoutRenderer>();
   });
   ```

### How to use ActivityTraceLayoutRenderer
The `System.Diagnostics.Activity.Current` from Microsoft allows one to create OpenTelemetry spans. 

Example of `NLog.config` file that outputs span-details together with LogEvent by using `${activity}`:

```xml
<nlog>
<extensions>
    <add assembly="NLog.DiagnosticSource"/>
</extensions>
<targets>
    <target name="console" xsi:type="console" layout="${message}|ActivityId=${activity:property=TraceId}" />
</targets>
<rules>
    <logger minLevel="Info" writeTo="console" />
</rules>
</nlog>
```

**Property Enum Values**
- Id : Hierarchical structure identifier that is concatenation of ParentIds
- SpanId : Identifier for the current activity (Ex. database activity within current request)
- ParentId : Identifier for the parent activity
- TraceId : Identifier for the root activity (Request Trace Identifier)
- OperationName : Operation name of the current activity
- DisplayName : Explicit assigned Activity DisplayName (with fallback to OperationName)
- StartTimeUtc : Time when the operation started
- Duration : Duration of the operation (formatted as TimeSpan)
- DurationMs : Duration of the operation (formatted as TimeSpan.TotalMilliseconds)
- Baggage : Collection of key/value pairs that are passed to children of this Activity (Use `Format="@"` for json-dictionary)
- Tags : Collection of key/value pairs that are NOT passed to children of this Activity (Use `Format="@"` for json-dictionary)
- CustomProperty : Custom property assigned to this activity. Must be used together with Item-option
- Events : Events attached to this activity (Use `Format="@"` for json-array)
- TraceState : W3C tracestate header
- TraceFlags : See System.Diagnostics.ActivityTraceFlags for activity (defined by the W3C ID specification). For int-value combine with `format="d"`
- SourceName : Name of the activity source associated with this activity
- SourceVersion : Version of the activity source associated with this activity
- ActivityKind : Relationship kind between the activity, its parents, and its children. For int-value combine with `format="d"`
- TraceStateString : W3C 'tracestate' header as a string
- Status : Status code of the current activity (`Ok` or `Error`). For int-value combine with `format="d"` 
- StatusDescription : Status description of the current activity
- IsAllDataRequested : Activity span has been marked to be sampled / recorded (`0` or `1`)

**Formatting**
- Format: Format for rendering the property.
- Culture: CultureInfo for rendering the property (Default Invariant Culture)
- Item: Lookup a single item from property-collection (Baggage, Tags, CustomProperty)
  - `${activity:property=Baggage:item=BaggageKey}`
  - `${activity:property=Tags:item=TagKey}`
  - `${activity:property=CustomProperty:item=PropertyKey}`

**Extract property values from parent or root**

It is possible to specify that the above property should be extracted from either root- or parent-activity.

```
${activity:property=OperationName:parent=true}
```

```
${activity:property=OperationName:root=true}
```

**Manually configure ActivityTrackingOptions**

When using the default HostBuilder then it will automatically setup the following [ActivityTrackingOptions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggerfactoryoptions.activitytrackingoptions):
```c#
builder.ConfigureLogging((hostingContext, loggingBuilder) =>
{
      loggingBuilder.Configure(options =>
      {
            options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                            | ActivityTrackingOptions.TraceId
                                            | ActivityTrackingOptions.ParentId;
      });
}).
```
If creating a custom HostBuilder, then one have to manually setup the ActivityTrackingOptions like shown above.

### How to use DiagnosticListenerTarget

Example of `NLog.config` file that uses the `diagnosticListener` target:

```xml
<nlog>
<extensions>
    <add assembly="NLog.DiagnosticSource"/>
</extensions>
<targets>
    <target name="diagSource" xsi:type="diagnosticListener" layout="${message}" sourceName="nlog" eventName="${logger}" />
</targets>
<rules>
    <logger minLevel="Info" writeTo="diagSource" />
</rules>
</nlog>
```

### SpanId / TraceId in custom NLog Target
NLog v5.3.4 introduced support for `Layout.FromMethod` that returns typed NLog `Layout<T>`. This allows custom NLog Targets to resolve SpanId / TraceId without have to parse from string or depend on `NLog.DiagnosticSource` nuget-package:
```csharp
public class MyCustomTarget : TargetWithContext
{
        private static readonly string EmptyTraceIdToHexString = default(System.Diagnostics.ActivityTraceId).ToHexString();
        private static readonly string EmptySpanIdToHexString = default(System.Diagnostics.ActivitySpanId).ToHexString();

        public Layout<System.Diagnostics.ActivityTraceId?> TraceId { get; set; } = Layout<System.Diagnostics.ActivityTraceId?>.FromMethod(static evt => System.Diagnostics.Activity.Current?.TraceId is System.Diagnostics.ActivityTraceId activityTraceId && !ReferenceEquals(EmptyTraceIdToHexString, activityTraceId.ToHexString()) ? activityTraceId : null);

        public Layout<System.Diagnostics.ActivitySpanId?> SpanId { get; set; } = Layout<System.Diagnostics.ActivitySpanId?>.FromMethod(static evt => System.Diagnostics.Activity.Current?.SpanId is System.Diagnostics.ActivitySpanId activitySpanId && !ReferenceEquals(EmptySpanIdToHexString, activitySpanId.ToHexString()) ? activitySpanId : null);

        protected override void Write(LogEventInfo logEvent)
        {
            var spanId = RenderLogEvent(SpanId, logEvent);
            if (spanId.HasValue)
                data.SpanId = spanId.Value;

            var traceId = RenderLogEvent(TraceId, logEvent);
            if (traceId.HasValue)
                data.TraceId = traceId.Value;
        }
}
```



