using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace WowMoment.Plugins.Base
{
    /// <summary>
    /// Base for all custom workflow activities. Same idea as PluginBase.
    /// Child classes only implement Run().
    /// </summary>
    public abstract class CodeActivityBase : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            var tracing = context.GetExtension<ITracingService>();
            var wfContext = context.GetExtension<IWorkflowContext>();
            var factory = context.GetExtension<IOrganizationServiceFactory>();
            var service = factory.CreateOrganizationService(wfContext.UserId);

            tracing.Trace("{0} started. Depth={1}", GetType().Name, wfContext.Depth);

            try
            {
                Run(context, service, tracing);
                tracing.Trace("{0} finished.", GetType().Name);
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace("Unexpected error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred in " + GetType().Name + ": " + ex.Message, ex);
            }
        }

        protected abstract void Run(CodeActivityContext context,
                                    IOrganizationService service,
                                    ITracingService tracing);
    }
}
