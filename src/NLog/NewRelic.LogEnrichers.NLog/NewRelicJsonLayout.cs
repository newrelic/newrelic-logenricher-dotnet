using NLog;
using NLog.Common;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Layouts;
using System;
using System.Text;

namespace NewRelic.LogEnrichers.NLog
{
    [Layout("newrelic-jsonlayout")]
    public class NewRelicJsonLayout : JsonLayout
    {
        internal const string TimestampLayoutRendererName = "nr-unix-timestamp";

        private readonly Lazy<NewRelic.Api.Agent.IAgent> _nrAgent;

        private IJsonConverter _jsonConverter;
        private IJsonConverter JsonConverter => _jsonConverter ?? (_jsonConverter = ConfigurationItemFactory.Default.JsonConverter);

        internal NewRelicJsonLayout(Func<NewRelic.Api.Agent.IAgent> agentFactory) : base()
        {
            _nrAgent = new Lazy<NewRelic.Api.Agent.IAgent>(agentFactory);
            LayoutRenderer.Register<UnixTimestampLayoutRenderer>(TimestampLayoutRendererName);


            SuppressSpaces = true;
            RenderEmptyObject = false;

            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.Timestamp.GetOutputName(), "${"+TimestampLayoutRendererName+"}", false));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.LogLevel.GetOutputName(), "${level:upperCase=true}", true));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.MessageText.GetOutputName(), "${message}", true));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.MessageTemplate.GetOutputName(), "${message:raw=true}"));

            // correlation
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.ThreadId.GetOutputName(), "${threadid}", true));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.CorrelationId.GetOutputName(), "${ActivityId}", true));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.ProcessId.GetOutputName(), "${processid}", true));

            // exceptions
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.ErrorClass.GetOutputName(), "${exception:format=Type}", true));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.ErrorMessage.GetOutputName(), "${exception:format=Message}", true));
            Attributes.Add(new JsonAttribute(NewRelicLoggingProperty.ErrorStack.GetOutputName(), "${exception:format=StackTrace}", true));

            //Nesting json objects like this works fine and will lead to message properties
            //that look like message.property.ErrorMessage in the UI.
            Attributes.Add(new JsonAttribute("Message Properties", new JsonLayout()
            {
                IncludeAllProperties = true,
                IncludeMdc = false,
                //IncludeGdc = false, // GDC not supported in NLog 4.5
                IncludeMdlc = false,
                RenderEmptyObject = false,
                SuppressSpaces = true,
                MaxRecursionLimit = MaxRecursionLimit,
                ExcludeProperties = ExcludeProperties
            }, false));
        }

        public NewRelicJsonLayout() : this(NewRelic.Api.Agent.NewRelic.GetAgent)
        {
        }

        //This prevents changing the properties that we don't want changed
        protected override void InitializeLayout()
        {
            // This reads XML configuration
            base.InitializeLayout();

            // Now we set things to how we want them configured finally

            //By not overriding the attributes collection here customers can add additional attributes
            //to the data, in a similar manner to how they would have added data via custom layout strings.
            //By default we will only support the data directly related to structured logging.
            //Note that any message properties will also be present in the Gdc, Mdc, and Mdlc contexts.
            IncludeAllProperties = false;
            //IncludeGdc = false; // GDC not supported in NLog 4.5
            IncludeMdc = false;
            IncludeMdlc = false;
            RenderEmptyObject = false;
            SuppressSpaces = true;
        }

        protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
        {
            const char JsonClose = '}';

            // calls in to the JsonLayout to render the json as a single object
            base.RenderFormattedMessage(logEvent, target);

            // removes the closing } to allow adding more data.
            target.Remove(target.Length - 1, 1);

            // adds linking data to json string
            // Not using a Renderer because these values need to be at the top level of the json.
            //It is safe to call this method here because we are using a custom layout, which NLog
            //assumes is not thread safe so it will render the layout before switching threads
            //when an async or buffered wrapper is used.
            if (_nrAgent.Value != null)
            {
                try
                {
                    var metadata = _nrAgent.Value.GetLinkingMetadata();
                    if (metadata != null)
                    {
                        foreach (var pair in metadata)
                        {
                            WriteJsonAttribute(pair.Key, pair.Value, target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    InternalLogger.Error(ex, "Exception caught in NewRelicJsonLayout.RenderFormattedMessage");
                }
            }

            target.Append(JsonClose);
        }

        // Writes out the json attributes using Utf8Json to ensure JSON is escaped as needed.
        private void WriteJsonAttribute(string name, string value, StringBuilder target)
        {
            target.Append($",\"{name}\":");
            // Uses the same JSON serializer as NLog
            JsonConverter.SerializeObject(value, target);
        }
    }

    [LayoutRenderer(NewRelicJsonLayout.TimestampLayoutRendererName)]
    public class UnixTimestampLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(logEvent.TimeStamp.ToUnixTimeMilliseconds());
        }
    }
}
