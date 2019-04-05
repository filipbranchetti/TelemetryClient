namespace Fb.TelemetryClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;

    public interface ITelemetryClient
    {
        Metric GetMetric(string metricId);
        Metric GetMetric(string metricId, string dimension1Name);
        Task<T> TrackDependencyAsync<T>(Func<Task<T>> task, DependencyKind dependencyKind, string hostName, string action, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null);
        void TrackEvent(string eventName, IDictionary<string, string> telemetryDetails, IDictionary<string, double> telemetryMetrics = null, string correlationId = null);
        void TrackException(Exception exception, Severity severity, IDictionary<string, string> telemetryDetails, IDictionary<string, double> telemetryMetrics = null, string correlationId = null);
        void TrackTrace(string message, Severity severity, IDictionary<string, string> telemetryDetails, string correlationId = null);
        Task TrackOperationAsync(Func<Task> task, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null);
        Task<T> TrackOperationAsync<T>(Func<Task<T>> task, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null);

        T TrackDependency<T>(Func<T> func, DependencyKind dependencyKind, string hostName, string action, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null);
        T TrackOperation<T>(Func<T> func, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null);
    }
}