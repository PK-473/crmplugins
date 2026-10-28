//using System;
//using Microsoft.Xrm.Sdk;

//namespace WowMoment.Plugins.Base
//{
//    /// <summary>
//    /// Base for all plugins. Gets the services, traces, and wraps unexpected errors.
//    /// Child classes only implement Run().
//    /// </summary>
//    public abstract class PluginBase : IPlugin
//    {
//        public void Execute(IServiceProvider serviceProvider)
//        {
//            if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");

//            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
//            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
//            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
//            var service = factory.CreateOrganizationService(context.UserId);

//            tracing.Trace("{0} started. Message={1}, Stage={2}, Depth={3}",
//                GetType().Name, context.MessageName, context.Stage, context.Depth);

//            try
//            {
//                Run(context, service, tracing);
//                tracing.Trace("{0} finished.", GetType().Name);
//            }
//            catch (InvalidPluginExecutionException)
//            {
//                throw; // our own business messages go to the user unchanged
//            }
//            catch (Exception ex)
//            {
//                tracing.Trace("Unexpected error: {0}", ex.ToString());
//                throw new InvalidPluginExecutionException(
//                    "An unexpected error occurred in " + GetType().Name + ": " + ex.Message, ex);
//            }
//        }

//        protected abstract void Run(IPluginExecutionContext context,
//                                    IOrganizationService service,
//                                    ITracingService tracing);
//    }
//}

using System;
using Microsoft.Xrm.Sdk;

namespace WowMoment.Plugins.Base
{
    /// <summary>
    /// Base class for every plugin in this project.
    /// It gets the services, writes the trace lines and handles errors in ONE place,
    /// so each plugin only contains its own business logic.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // Act as the user who called the action
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
                // Already a friendly message for the caller: pass it on unchanged
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace("Unexpected error in {0}: {1}", GetType().Name, ex.ToString());
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred while processing the request. " +
                    "Please try again, or contact your administrator if it continues. (" + GetType().Name + ")", ex);
            }
        }

        /// <summary>Each plugin implements only this method.</summary>
        protected abstract void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing);
    }
}
