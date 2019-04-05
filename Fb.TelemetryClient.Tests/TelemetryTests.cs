namespace Fb.TelemetryClient.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    using Moq;

    using NUnit.Framework;

    using Shouldly;


    [TestFixture]
    public class TelemetryTests
    {
        private readonly Mock<ITelemetryChannel> _telemetryChannelMock = new Mock<ITelemetryChannel>();
        private readonly TelemetryClient _telemetryClient;

        public TelemetryTests()
        {
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = _telemetryChannelMock.Object,
                InstrumentationKey = Guid.NewGuid().ToString(),
            };

            _telemetryClient = new TelemetryClient(configuration);
        }

        [TearDown]
        public void TearDown()
        {
            _telemetryChannelMock.Reset();
        }

        [Test]
        public void Given_null_parameter_when_constructing_then_should_throw()
        {
            Should.Throw<ArgumentNullException>(() => new TelemetryClient(null));
            Should.Throw<ArgumentNullException>(() => new TelemetryClient((string)null, "System Name"));
        }

        [Test]
        public void Given_configuration_when_constructing_should_handle_existing_initializer()
        {
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = _telemetryChannelMock.Object,
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryInitializers = { new OperationCorrelationTelemetryInitializer(), new TelemetryInitializer("test") },
            };

            Should.NotThrow(() => new TelemetryClient(configuration));
            configuration.TelemetryInitializers.Count.ShouldBe(2);
        }

        [Test]
        public void Given_configuration_with_only_one_initializer_when_constructing_then_should_add_initializer()
        {
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = _telemetryChannelMock.Object,
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryInitializers = { new OperationCorrelationTelemetryInitializer() },
            };

            Should.NotThrow(() => new TelemetryClient(configuration));
            configuration.TelemetryInitializers.Count.ShouldBe(2);
        }

        [Test]
        public void Given_correct_parameters_when_constructing_then_should_not_throw()
        {
            Assert.DoesNotThrow( () =>
            {
                new TelemetryClient("key", "name");
            });
        }

        [Test]
        public void Given_information_for_initializer_should_add_property()
        {
            var sourceSystemName = "Test Name";
            var initializer = new TelemetryInitializer(sourceSystemName);
            var telemetry = new TraceTelemetry();
            initializer.Initialize(telemetry);

            telemetry.Context.GlobalProperties.ShouldContainKeyAndValue("sourceSystemName", sourceSystemName);
        }

        [TestCase(Severity.Verbose, SeverityLevel.Verbose, "verbose message")]
        [TestCase(Severity.Information, SeverityLevel.Information, "information message")]
        [TestCase(Severity.Warning, SeverityLevel.Warning, "warning message")]
        [TestCase(Severity.Error, SeverityLevel.Error, "error message")]
        [TestCase(Severity.Critical, SeverityLevel.Critical, "critical message")]
        public void Given_severity_and_message_when_logging_then_should_track_trace(Severity givenSeverityLevel, SeverityLevel expectedSeverityLevel, string message)
        {
            _telemetryClient.TrackTrace(message, givenSeverityLevel, new Dictionary<string, string>
            {
                { "traceCategory", "category" },
            });

            _telemetryChannelMock.Verify(m => m.Send(It.Is<TraceTelemetry>(t => t.SeverityLevel == expectedSeverityLevel && t.Message == message && t.Properties["traceCategory"] == "category")), Times.Once);
        }

        [Test]
        public void Given_exception_and_severity_then_should_track_exception()
        {
            _telemetryClient.TrackException(new ArgumentException("Invalid argument"), Severity.Error, new Dictionary<string, string>
            {
                { "traceCategory", "category" },
            });

            _telemetryChannelMock.Verify(m => m.Send(It.Is<ExceptionTelemetry>(t => t.SeverityLevel == SeverityLevel.Error && t.Exception is ArgumentException && t.Properties["traceCategory"] == "category")), Times.Once);
        }

  

        [Test]
        public void Given_custom_event_information_then_should_track_event()
        {
            _telemetryClient.TrackEvent("Test", new Dictionary<string, string>
            {
                { "traceCategory", "category" },
            }, new Dictionary<string, double> { { "metric", 14d } });

            _telemetryChannelMock.Verify(m => m.Send(It.Is<EventTelemetry>(t => t.Name == "Test" && t.Properties["traceCategory"] == "category")), Times.Once);
        }

        [Test]
        public void Given_external_dependency_then_should_track_dependency_call()
        {
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

            _telemetryChannelMock.Verify(m => m.Send(It.Is<DependencyTelemetry>(t => t.Name == hostname && t.Data == operationName && t.Target == action && t.Success == true && t.Properties["traceCategory"] == "category")), Times.Once);
        }

        [Test]
        public async Task Given_external_async_dependency_then_should_track_dependency_call()
        {
            var hostname = "localhost";
            var action = "POST /Task/Delay";
            var operationName = "Task.Delay()";
            await _telemetryClient.TrackDependencyAsync(async () =>
            {
                await Task.Delay(0);
                return true;
            }, DependencyKind.Http, hostname, action, operationName, new Dictionary<string, string>
            {
                { "traceCategory", "category" },
            });

            _telemetryChannelMock.Verify(m => m.Send(It.Is<DependencyTelemetry>(t => t.Name == hostname && t.Data == operationName && t.Target == action && t.Success == true && t.Properties["traceCategory"] == "category")), Times.Once);
        }

        [Test]
        public void Given_external_dependency_with_invalid_category_key_then_should_throw()
        {
            Should.Throw<ArgumentNullException>( 
                () => _telemetryClient.TrackDependency( () =>
            {
                return true;
            }, DependencyKind.Service, string.Empty, string.Empty, "Task.Delay()", new Dictionary<string, string>
            {
                { null, "category" },
            }));
        }

        [Test]
        public void Given_external_async_dependency_with_invalid_category_key_then_should_throw()
        {
            Should.Throw<ArgumentNullException>(async () => await _telemetryClient.TrackDependencyAsync(async () =>
            {
                await Task.Delay(0);
                return true;
            }, DependencyKind.Service, string.Empty, string.Empty, "Task.Delay()", new Dictionary<string, string>
            {
                { null, "category" },
            }));
        }

        [Test]
        public void Given_external_dependency_with_invalid_category_key_then_dependency_success_should_be_false()
        {
            try
            {
                Should.Throw<OperationCanceledException>(async () => await _telemetryClient.TrackDependencyAsync(async () =>
                {
                    await Task.Delay(0, new CancellationToken(true));
                    return true;
                }, DependencyKind.Http, "localhost", string.Empty, "Task.Delay()", new Dictionary<string, string>
                {
                    { "traceCategory", "category" },
                }));
            }
            finally
            {
                _telemetryChannelMock.Verify(m => m.Send(It.Is<DependencyTelemetry>(t => t.Name == "localhost" && t.Success == false)), Times.Once);
            }
        }

        [Test]
        public void Given_the_start_of_a_new_complex_operation_then_should_track_dependency_and_keep_correlating_information_together()
        {
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

            _telemetryChannelMock.Verify(m => m.Send(It.IsAny<ITelemetry>()), Times.Exactly(3));
            _telemetryChannelMock.Verify(m => m.Send(It.Is<RequestTelemetry>(t => t.Context.Operation.Id == parentId)), Times.Once);
            _telemetryChannelMock.Verify(m => m.Send(It.Is<ITelemetry>(t => t.Context.Operation.ParentId == parentId)), Times.Exactly(3));
        }


        [Test]
        public async Task Given_the_start_of_a_new_complex_async_operation_then_should_track_dependency_and_keep_correlating_information_together()
        {
            const string traceCategory = "category";
            const string parentId = "123";
            await _telemetryClient.TrackOperationAsync(async () =>
            {
                _telemetryClient.TrackTrace("Tracing test run", Severity.Information, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, parentId);
                await Task.Delay(0);
                _telemetryClient.TrackException(new ArgumentException("invalid argument"), Severity.Error, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, new Dictionary<string, double> { { "metric", 14d } }, parentId);
            }, "testing multiple calls", new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, parentId);

            _telemetryChannelMock.Verify(m => m.Send(It.IsAny<ITelemetry>()), Times.Exactly(3));
            _telemetryChannelMock.Verify(m => m.Send(It.Is<RequestTelemetry>(t => t.Context.Operation.Id == parentId)), Times.Once);
            _telemetryChannelMock.Verify(m => m.Send(It.Is<ITelemetry>(t => t.Context.Operation.ParentId == parentId)), Times.Exactly(3));
        }


        [Test]
        public void Given_the_start_of_a_new_complex_async_operation_that_fails_then_should_track_dependency_with_failure()
        {
            var parentId = "123";
            Should.Throw<OperationCanceledException>(async () => await _telemetryClient.TrackOperationAsync(async () =>
            {
                var traceCategory = "category";
                _telemetryClient.TrackTrace("Tracing test run", Severity.Information, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, parentId);
                await Task.Delay(0, new CancellationToken(true));
                _telemetryClient.TrackException(new ArgumentException("should not get here"), Severity.Error, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, null, parentId);
            }, "testing multiple calls", new Dictionary<string, string>()));

            _telemetryChannelMock.Verify(m => m.Send(It.IsAny<ITelemetry>()), Times.Exactly(2));
        }

        [Test]
        public async Task Given_a_telemetry_item_from_service_bus_with_unwanted_data_then_should_not_track()
        {
            await _telemetryClient.TrackDependencyAsync(async () =>
            {
                await Task.Delay(0, CancellationToken.None);
                return true;
            }, DependencyKind.AzureServiceBus, "Receive", "action", "operation", new Dictionary<string, string>
            {
                { "traceCategory", "category" },
            });

            _telemetryChannelMock.Verify(m => m.Send(It.IsAny<ITelemetry>()), Times.Never);
        }

        [Test]
        public void Given_correlation_id_only_as_parameter_should_still_give_correlation_id_in_properties()
        {
            const string traceCategory = "category";
            const string correlationId = "123";

            _telemetryClient.TrackTrace("Tracing test run", Severity.Information, new Dictionary<string, string> { { nameof(traceCategory), traceCategory } }, correlationId);

            _telemetryChannelMock.Verify(m => m.Send(It.Is<ITelemetry>(t => t.Context.GlobalProperties["correlationId"] == correlationId)), Times.Once);
        }
    }
}
