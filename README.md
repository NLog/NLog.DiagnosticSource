# NLog.DiagnosticSource
NLog DiagnosticListenerTarget for [Microsoft DiagnosticSource](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)

NLog ActivityTraceLayoutRenderer for [Microsoft Activity Trace](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md)

[![Version](https://badge.fury.io/nu/NLog.DiagnosticSource.svg)](https://www.nuget.org/packages/NLog.DiagnosticSource)
[![AppVeyor](https://img.shields.io/appveyor/ci/nlog/NLog.DiagnosticSource/master.svg)](https://ci.appveyor.com/project/nlog/NLog.DiagnosticSource/branch/master)

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
    <target name="console" xsi:type="console" layout="${message}|ActivityId=${activity:property=Id}" />
</targets>
<rules>
    <logger minLevel="Info" writeTo="console" />
</rules>
```

The follow property values can be used:
- Id : Identifier that is specific to a particular request.
- TraceId : TraceId part of the Id
- SpanId : SPAN part of the Id
- OperationName : Operation name
- StartTimeUtc : Time when the operation started
- Duration : Duration of the operation
- Baggage : Collection of key/value pairs that are passed to children of this Activity
- Tags : Collection of key/value pairs that are NOT passed to children of this Activity
- ParentId : Activity's Parent ID
- ParentSpanId : Activity's Parent SpanID
- RootId : Root ID of this Activity
- TraceState : W3C tracestate header
- ActivityTraceFlags : See System.Diagnostics.ActivityTraceFlags for activity (defined by the W3C ID specification) 