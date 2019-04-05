namespace Fb.TelemetryClient
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public class TelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _telemetryProcessor;

        public TelemetryProcessor(ITelemetryProcessor telemetryProcessor)
        {
            _telemetryProcessor = telemetryProcessor;
        }

        public void Process(ITelemetry item)
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();

            switch (item)
            {
                case DependencyTelemetry dependencyTelemetry:
                    if (dependencyTelemetry.Type.Equals("Azure Service Bus", StringComparison.OrdinalIgnoreCase) && dependencyTelemetry.Name.Equals("Receive", StringComparison.OrdinalIgnoreCase))
                        return;

                    properties = dependencyTelemetry.Properties;
                    break;
                case ExceptionTelemetry exceptionTelemetry:
                    properties = exceptionTelemetry.Properties;
                    break;
                case EventTelemetry eventTelemetry:
                    properties = eventTelemetry.Properties;
                    break;
                case TraceTelemetry traceTelemetry:
                    properties = traceTelemetry.Properties;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(item?.Context?.Operation?.Name) && !IsCorrelationIdOnTelemetry(properties, item.Context?.GlobalProperties))
            {
                item.Context.GlobalProperties.Add("correlationId", item.Context.Operation.Id);
            }

            _telemetryProcessor.Process(item);
        }

        private static bool IsCorrelationIdOnTelemetry(IDictionary<string, string> properties, IDictionary<string, string> globalProperties)
        {
            const string correlationId = "correlationId";
            return properties != null && properties.ContainsKey(correlationId) || globalProperties.ContainsKey(correlationId);
        }
    }
}