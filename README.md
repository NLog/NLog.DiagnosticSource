# NLog.DiagnosticSource
NLog DiagnosticListenerTarget for [Microsoft DiagnosticSource](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)

NLog ActivityTraceLayoutRenderer for [Microsoft Activity Trace](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md)

[![Version](https://badge.fury.io/nu/NLog.DiagnosticSource.svg)](https://www.nuget.org/packages/NLog.DiagnosticSource)
[![AppVeyor](https://img.shields.io/appveyor/ci/nlog/NLog-DiagnosticSource/master.svg)](https://ci.appveyor.com/project/nlog/NLog-DiagnosticSource/branch/master)

### How to install

1) Install the package

    `Install-Package NLog.DiagnosticSource` or in your csproj:

    ```xml
    <PackageReference Include="NLog.DiagnosticSource" Version="1.0.0" />
    ```

2) Add to your nlog.config:

    ```xml
    <extensions>
        <add assembly="NLog.DiagnosticSource"/>
    </extensions>
    ```

### How to use DiagnosticListenerTarget

Use the target "diagnosticListener" in your nlog.config

```xml
<extensions>
    <add assembly="NLog.DiagnosticSource"/>
</extensions>
<targets>
    <target name="diagSource" xsi:type="diagnosticListener" layout="${message}" sourceName="nlog" eventName="${logger}" />
</targets>
<rules>
    <logger minLevel="Info" writeTo="diagSource" />
</rules>
```

### How to use ActivityTraceLayoutRenderer

Use the layout "${activity}" in your nlog.config

```xml
<extensions>
    <add assembly="NLog.DiagnosticSource"/>
</extensions>
<targets>
    <target name="console" xsi:type="console" layout="${message}|ActivityId=${activity:property=TraceId}" />
</targets>
<rules>
    <logger minLevel="Info" writeTo="console" />
</rules>
```

**Property Values**
- SpanId : Identifier for the current activity
- TraceId : Identifier for the root activity
- ParentId : Identifier for the parent activity
- OperationName : Operation name of the current activity
- StartTimeUtc : Time when the operation started
- Duration : Duration of the operation
- Baggage : Collection of key/value pairs that are passed to children of this Activity
- Tags : Collection of key/value pairs that are NOT passed to children of this Activity
- Events : Events attached to this activity
- CustomProperty : Custom property assigned to this activity. Must be used together with Item-option
- TraceState : W3C tracestate header
- TraceFlags : See System.Diagnostics.ActivityTraceFlags for activity (defined by the W3C ID specification) 
- SourceName : Name of the activity source associated with this activity
- SourceVersion : Version of the activity source associated with this activity
- ActivityKind : Relationship kind between the activity, its parents, and its children
