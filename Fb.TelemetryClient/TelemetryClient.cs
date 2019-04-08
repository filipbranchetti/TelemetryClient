namespace Fb.TelemetryClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public class TelemetryClient : ITelemetryClient
    {
        private readonly Microsoft.ApplicationInsights.TelemetryClient _telemetryClient;

        public TelemetryClient()
        {
            _telemetryClient = Initialize(TelemetryConfiguration.Active,"");
        }
        public TelemetryClient(string applicationInsightsKey, string sourceSystemName)
        {
            var telemetryConfiguration = new TelemetryConfiguration(applicationInsightsKey ?? throw new ArgumentNullException(nameof(applicationInsightsKey)));
            _telemetryClient = Initialize(telemetryConfiguration, sourceSystemName);
        }

        public TelemetryClient(TelemetryConfiguration telemetryConfiguration, string sourceSystemName = "")
        {
            _telemetryClient = Initialize(telemetryConfiguration ?? throw new ArgumentNullException(nameof(telemetryConfiguration)), sourceSystemName);
        }

        private static Microsoft.ApplicationInsights.TelemetryClient Initialize(TelemetryConfiguration telemetryConfiguration, string sourceSystemName)
        {
            if (!telemetryConfiguration.TelemetryInitializers.Any(i => i is OperationCorrelationTelemetryInitializer))
                telemetryConfiguration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            if (!telemetryConfiguration.TelemetryInitializers.Any(i => i is TelemetryInitializer))
                telemetryConfiguration.TelemetryInitializers.Add(new TelemetryInitializer(sourceSystemName ?? throw new ArgumentNullException(nameof(sourceSystemName))));

            var telemetryProcessorChainBuilder = telemetryConfiguration.TelemetryProcessorChainBuilder;
            telemetryProcessorChainBuilder.Use(telemetryProcessor => new TelemetryProcessor(telemetryProcessor));
            telemetryProcessorChainBuilder.Build();

            return new Microsoft.ApplicationInsights.TelemetryClient(telemetryConfiguration);
        }

        public async Task TrackOperationAsync(Func<Task> task, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null)
        {
            await TrackOperationAsync(async () =>
            {
                await task();
                return true;
            }, operationName, telemetryDetails, correlationId);
        }

        public T TrackDependency<T>(Func<T> func, DependencyKind dependencyKind, string hostName, string action, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null)
        {
            var successful = true;
            var dependencyName = GetDependencyNameFromKind(dependencyKind);

            var dependencyTelemetry = new DependencyTelemetry(dependencyName, action, hostName, operationName);
            try
            {
                PopulateProperties(dependencyTelemetry, telemetryDetails, correlationId);
                dependencyTelemetry.Start();
                var result = func();
                return result;
            }
            catch (Exception)
            {
                successful = false;
                throw;
            }
            finally
            {
                dependencyTelemetry.Stop();
                dependencyTelemetry.Success = successful;
                _telemetryClient.TrackDependency(dependencyTelemetry);
            }
        }

        public T TrackOperation<T>(Func<T> func, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null)
        {
            var successful = true;
            using (var operation = _telemetryClient.StartOperation<RequestTelemetry>(operationName, correlationId))
            {
                try
                {
                    PopulateProperties(operation.Telemetry, telemetryDetails, correlationId);

                    var result = func();
                    return result;
                }
                catch (Exception)
                {
                    successful = false;
                    throw;
                }
                finally
                {
                    operation.Telemetry.Success = successful;
                }
            }
        }

        public async Task<T> TrackOperationAsync<T>(Func<Task<T>> task, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null)
        {
            var successful = true;
            using (var operation = _telemetryClient.StartOperation<RequestTelemetry>(operationName, correlationId))
            {
                try
                {
                    PopulateProperties(operation.Telemetry, telemetryDetails, correlationId);

                    var result = await task();
                    return result;
                }
                catch (Exception)
                {
                    successful = false;
                    throw;
                }
                finally
                {
                    operation.Telemetry.Success = successful;
                }
            }
        }

   
        public async Task<T> TrackDependencyAsync<T>(Func<Task<T>> task, DependencyKind dependencyKind, string hostName, string action, string operationName, IDictionary<string, string> telemetryDetails, string correlationId = null)
        {
            var successful = true;
            var dependencyName = GetDependencyNameFromKind(dependencyKind);

            var dependencyTelemetry = new DependencyTelemetry(dependencyName, action, hostName, operationName);
            try
            {
                PopulateProperties(dependencyTelemetry, telemetryDetails, correlationId);
                dependencyTelemetry.Start();
                var result = await task();
                return result;
            }
            catch (Exception)
            {
                successful = false;
                throw;
            }
            finally
            {
                dependencyTelemetry.Stop();
                dependencyTelemetry.Success = successful;
                _telemetryClient.TrackDependency(dependencyTelemetry);
            }
        }

        private string GetDependencyNameFromKind(DependencyKind dependencyKind)
        {
            var memberInfo = dependencyKind.GetType().GetMember(dependencyKind.ToString("G"));
            var attributes = memberInfo.First().GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((DescriptionAttribute)attributes[0]).Description;
        }

        public void TrackException(Exception exception, Severity severity, IDictionary<string, string> telemetryDetails, IDictionary<string, double> telemetryMetrics = null, string correlationId = null)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception);
            PopulateProperties(exceptionTelemetry, telemetryDetails, correlationId);
            PopulateMetrics(exceptionTelemetry.Metrics, telemetryMetrics);
            exceptionTelemetry.SeverityLevel = (SeverityLevel)severity;

            _telemetryClient.TrackException(exceptionTelemetry);
        }

        [ExcludeFromCodeCoverage]
        public Metric GetMetric(string metricId)
        {
            return _telemetryClient.GetMetric(metricId);
        }

        [ExcludeFromCodeCoverage]
        public Metric GetMetric(string metricId, string dimension1Name)
        {
            return _telemetryClient.GetMetric(metricId, dimension1Name);
        }

        public void TrackEvent(string eventName, IDictionary<string, string> telemetryDetails, IDictionary<string, double> telemetryMetrics = null, string correlationId = null)
        {
            var eventTelemetry = new EventTelemetry(eventName);
            PopulateProperties(eventTelemetry, telemetryDetails, correlationId);
            PopulateMetrics(eventTelemetry.Metrics, telemetryMetrics);

            _telemetryClient.TrackEvent(eventTelemetry);
        }

        private static void PopulateMetrics(IDictionary<string, double> metrics, IDictionary<string, double> telemetryMetrics)
        {
            if (telemetryMetrics == null)
                return;

            foreach (var telemetryMetric in telemetryMetrics)
            {
                metrics.Add(telemetryMetric);
            }
        }

        public void TrackTrace(string message, Severity severity, IDictionary<string, string> telemetryDetails, string correlationId = null)
        {
            var traceTelemetry = new TraceTelemetry(message, (SeverityLevel)severity);
            PopulateProperties(traceTelemetry, telemetryDetails, correlationId);

            _telemetryClient.TrackTrace(traceTelemetry);
        }

        private static void PopulateProperties(ITelemetry telemetry, IDictionary<string, string> telemetryDetails, string correlationId)
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();

            switch (telemetry)
            {
                case DependencyTelemetry dependencyTelemetry:
                    properties = dependencyTelemetry.Properties; break;
                case ExceptionTelemetry exceptionTelemetry:
                    properties = exceptionTelemetry.Properties; break;
                case EventTelemetry eventTelemetry:
                    properties = eventTelemetry.Properties; break;
                case TraceTelemetry traceTelemetry:
                    properties = traceTelemetry.Properties; break;
            }

            foreach (var property in telemetryDetails)
            {
                properties.Add(property);
            }

            if (string.IsNullOrWhiteSpace(correlationId))
                return;

            telemetry.Context.Operation.ParentId = correlationId;
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(correlationId)))
                telemetry.Context.GlobalProperties.Add(nameof(correlationId), correlationId);
        }
    }
}