using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nsquare.Crm.Customizations.Plugin
{
    public class ContactPostCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracer service
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth > 1)
                return;

            if (context.MessageName == "Create" &&
                context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                Entity contact = (Entity)context.InputParameters["Target"];

                string firstName = contact.GetAttributeValue<string>("firstname");

                tracer.Trace("Testing Contact Firstname: " + firstName);

                Entity updateContact = new Entity("contact");
                updateContact.Id = contact.Id;
                updateContact["firstname"] = "IND-" + firstName;

                service.Update(updateContact);
            }
        }
    }
}
