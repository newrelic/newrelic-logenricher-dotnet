using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;
using NewRelic.Api.Agent;
using System;
using System.Text;
using System.Threading;

namespace NewRelic.LogEnrichers.NLog
{
    [Layout("newrelic-jsonlayout")]
    public class NewRelicJsonLayout : JsonLayout
    {
        private readonly Lazy<NewRelic.Api.Agent.IAgent> _nrAgent;

        private IJsonConverter _jsonConverter;
        private IJsonConverter JsonConverter => _jsonConverter ?? (_jsonConverter = ConfigurationItemFactory.Default.JsonConverter);

        internal NewRelicJsonLayout(Func<NewRelic.Api.Agent.IAgent> agentFactory) : base()
        {
            _nrAgent = new Lazy<NewRelic.Api.Agent.IAgent>(agentFactory);

            SuppressSpaces = true;
            RenderEmptyObject = false;

            // We do not want the properties written automatically since we want to add a prefix
            //IncludeAllProperties = true;

            Attributes.Add(new JsonAttribute("timestamp", "${unix-timestamp}", false));
            Attributes.Add(new JsonAttribute("log.level", "${level:upperCase=true}", true));
            Attributes.Add(new JsonAttribute("message", "${message}", true));
            Attributes.Add(new JsonAttribute("message.template", "${message:raw=true}"));

            // correlation
            Attributes.Add(new JsonAttribute("thread.id", "${threadid}", true));
            Attributes.Add(new JsonAttribute("correlation.id", "${ActivityId}", true));
            Attributes.Add(new JsonAttribute("process.id", "${processid}", true));
            Attributes.Add(new JsonAttribute("line.number", "${callsite-linenumber}", true));

            // exceptions
            Attributes.Add(new JsonAttribute("error.class", "${exception:format=Type}", true));
            Attributes.Add(new JsonAttribute("error.message", "${exception:format=Message}", true));
            Attributes.Add(new JsonAttribute("error.stack", "${exception:format=StackTrace}", true));

            //Nesting json objects like this works fine and will lead to message properties
            //that look like message.property.ErrorMessage in the UI.
            Attributes.Add(new JsonAttribute("message.property", new JsonLayout()
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

            //Do not include the following pieces of data until requested

            //Attributes.Add(new JsonAttribute("message.mdc", new JsonLayout()
            //{
            //	IncludeAllProperties = false,
            //	IncludeMdc = true,
            //	IncludeGdc = false,
            //	IncludeMdlc = false,
            //	RenderEmptyObject = false,
            //	SuppressSpaces = true
            //}, false));

            //Attributes.Add(new JsonAttribute("message.mdlc", new JsonLayout()
            //{
            //	IncludeAllProperties = true,
            //	IncludeMdc = false,
            //	IncludeGdc = false,
            //	IncludeMdlc = true,
            //	RenderEmptyObject = false,
            //	SuppressSpaces = true
            //}, false));

            //Attributes.Add(new JsonAttribute("message.gdc", new JsonLayout()
            //{
            //	IncludeAllProperties = true,
            //	IncludeMdc = false,
            //	IncludeGdc = true,
            //	IncludeMdlc = false,
            //	RenderEmptyObject = false,
            //	SuppressSpaces = true
            //}, false));
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
            var metadata = _nrAgent.Value.GetLinkingMetadata();
            foreach (var pair in metadata)
            {
                WriteJsonAttribute(pair.Key, pair.Value, target);
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

    [LayoutRenderer("unix-timestamp")]
    public class UnixTimestampLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(logEvent.TimeStamp.ToUnixTimeMilliseconds());
        }
    }
}
