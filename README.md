# TelemetryClient
An easy tool for logging to Application insight

## What is the MigrationManager project?
```
PM> Install-Package fb.TelemetryClient -Version 1.0.0
```

TelemetryClient project is an service for handling traces to application insight.


## Examples

for more examples check the unit test file.

```
  _telemetryClient.TrackTrace(message, givenSeverityLevel, new Dictionary<string, string>
        {
            { "traceCategory", "category" },
        });
```
Track dependency
```
    var hostname = "localhost";
        var action = "POST /Task/Delay";
        var operationName = "Task.Delay()";
        _telemetryClient.TrackDependency( () =>
        {
            return true;
        }, DependencyKind.Http, hostname, action, operationName, new Dictionary<string, string>
        {
            { "traceCategory", "category" },
        });
        
```

Operation and nested tracks
```
         const string traceCategory = "category";
            const string parentId = "123";
            _telemetryClient.TrackOperation( () =>
            {
                _telemetryClient.TrackTrace("Tracing test run", Severity.Information, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, parentId);
                _telemetryClient.TrackException(new ArgumentException("invalid argument"), Severity.Error, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, new Dictionary<string, double> { { "metric", 14d } }, parentId);
                return "done";
            }, 
                "testing multiple calls", 
                new Dictionary<string, string> { { nameof(traceCategory), traceCategory } },
                parentId);
```



