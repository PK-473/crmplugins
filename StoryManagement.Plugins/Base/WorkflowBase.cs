using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace StoryManagement.Plugins.Base
{
    public abstract class WorkflowBase : CodeActivity
    {
        protected sealed override void Execute(
            CodeActivityContext context)
        {
            var tracing =
                context.GetExtension<ITracingService>();

            var workflowContext =
                context.GetExtension<IWorkflowContext>();

            var factory =
                context.GetExtension<IOrganizationServiceFactory>();

            var service =
                factory.CreateOrganizationService(
                    workflowContext.UserId);

            tracing.Trace(
                "{0} started.",
                GetType().Name);

            try
            {
                Run(
                    context,
                    workflowContext,
                    service,
                    tracing);

                tracing.Trace(
                    "{0} completed successfully.",
                    GetType().Name);
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace(
                    ex.ToString());

                throw new InvalidPluginExecutionException(
                    "Unexpected error in " +
                    GetType().Name,
                    ex);
            }
        }

        protected abstract void Run(
            CodeActivityContext context,
            IWorkflowContext workflowContext,
            IOrganizationService service,
            ITracingService tracing);
    }
}