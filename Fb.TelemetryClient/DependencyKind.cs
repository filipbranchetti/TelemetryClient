namespace Fb.TelemetryClient
{
    using System.ComponentModel;

    public enum DependencyKind
    {
        [Description("HTTP")]
        Http,
        [Description("Web Service")]
        WebService,
        [Description("Service")]
        Service,
        [Description("Azure Service Bus")]
        AzureServiceBus,
    }
}