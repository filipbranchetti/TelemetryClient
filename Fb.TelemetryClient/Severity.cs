namespace Fb.TelemetryClient
{
    using Microsoft.ApplicationInsights.DataContracts;

    public enum Severity
    {
        Verbose = SeverityLevel.Verbose,
        Information = SeverityLevel.Information,
        Warning = SeverityLevel.Warning,
        Error = SeverityLevel.Error,
        Critical = SeverityLevel.Critical,
    }
}