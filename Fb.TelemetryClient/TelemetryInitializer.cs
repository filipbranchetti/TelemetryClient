namespace Fb.TelemetryClient
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public class TelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _sourceSystemName;

        public TelemetryInitializer(string sourceSystemName)
        {
            _sourceSystemName = sourceSystemName ?? throw new ArgumentNullException(nameof(sourceSystemName));
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (!telemetry.Context.GlobalProperties.ContainsKey("sourceSystemName"))
            {
                telemetry.Context.GlobalProperties.Add("sourceSystemName", _sourceSystemName);
            }
        }
    }
}