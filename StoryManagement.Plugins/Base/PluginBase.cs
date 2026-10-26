using System;
using Microsoft.Xrm.Sdk;

namespace StoryManagement.Plugins.Base
{
    /// <summary>
    /// Base class for every plugin in this project.
    /// It gets the services, writes the trace lines, and handles errors in ONE place,
    /// so each plugin class only contains its own business logic.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            // 1. Ask Dataverse for the services we need
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // 2. Create a service that acts as the user who triggered the plugin
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            tracing.Trace("{0} started. Message={1}, Stage={2}, Mode={3}, Depth={4}",
                GetType().Name, context.MessageName, context.Stage, context.Mode, context.Depth);

            try
            {
                Run(context, service, tracing);
                tracing.Trace("{0} finished successfully.", GetType().Name);
            }
            catch (InvalidPluginExecutionException)
            {
                // Already a friendly message for the user: pass it on unchanged
                throw;
            }
            catch (Exception ex)
            {
                // Unexpected problem: keep the details in the trace log,
                // show the user a short message
                tracing.Trace("Unexpected error in {0}: {1}", GetType().Name, ex.ToString());
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred while processing the story. " +
                    "Please try again, or contact your administrator if it continues. (" + GetType().Name + ")", ex);
            }
        }

        /// <summary>Each plugin implements only this method.</summary>
        protected abstract void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing);
    }
}