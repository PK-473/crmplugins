using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nsquare.Crm.Customizations.Plugin
{
    public class GetSet : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracer service
            ITracingService tracer =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context
            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth > 1)
            {
                return;
            }

            try
            {
                if (context.MessageName == "Create"
                    && context.InputParameters.Contains("Target")
                    && context.InputParameters["Target"] is Entity)
                {
                    Entity contact = (Entity)context.InputParameters["Target"];

                    // =========================
                    // GET VALUES
                    // =========================

                    string firstName =
                        contact.GetAttributeValue<string>("firstname");

                    string lastName =
                        contact.GetAttributeValue<string>("lastname");

                    string email =
                        contact.GetAttributeValue<string>("emailaddress1");

                    EntityReference parentCustomer =
                        contact.GetAttributeValue<EntityReference>("parentcustomerid");

                    OptionSetValue preferredContactMethod =
                        contact.GetAttributeValue<OptionSetValue>("preferredcontactmethodcode");

                    DateTime? birthDate =
                        contact.GetAttributeValue<DateTime?>("birthdate");

                    // =========================
                    // TRACE VALUES
                    // =========================

                    tracer.Trace("First Name : " + firstName);
                    tracer.Trace("Last Name : " + lastName);
                    tracer.Trace("Email : " + email);

                    if (parentCustomer != null)
                    {
                        tracer.Trace("Parent Customer : " + parentCustomer.Name);
                    }

                    if (preferredContactMethod != null)
                    {
                        tracer.Trace("Preferred Contact Method : "
                            + preferredContactMethod.Value);
                    }

                    if (birthDate.HasValue)
                    {
                        tracer.Trace("Birth Date : "
                            + birthDate.Value.ToShortDateString());
                    }

                    // =========================
                    // SET VALUES
                    // =========================

                    Entity contactToUpdate = new Entity("contact");
                    contactToUpdate.Id = contact.Id;

                    // Multiple Lines of Text
                    contactToUpdate["description"] =
                        $"PostOperation executed for {firstName} {lastName}";

                    // Single Line of Text
                    contactToUpdate["jobtitle"] =
                        "Dynamics 365 Developer";

                    // Two Options
                    contactToUpdate["creditonhold"] =
                        false;

                    // Choice
                    contactToUpdate["preferredcontactmethodcode"] =
                        new OptionSetValue(2);

                    // Email
                    contactToUpdate["emailaddress2"] =
                        "secondary@test.com";

                    service.Update(contactToUpdate);

                    tracer.Trace("Contact updated successfully.");
                }
            }
            catch (Exception ex)
            {
                tracer.Trace("Error: " + ex.ToString());
                throw;
            }
        }
    }
}
