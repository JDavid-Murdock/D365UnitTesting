using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

// Microsoft Dynamics CRM namespace(s)
using Microsoft.Xrm.Sdk;

namespace D365.Samples.Plugin
{
    public class SamplePlugin : IPlugin
    {
        /// <summary>
        /// A plug-in that does something when some event occurs.
        /// </summary>
        /// <remarks>
        /// Register this plug-in on some message, some entity,
        /// </remarks>
        public void Execute(IServiceProvider serviceProvider)
        {
                        
            //Extract the tracing service for use in debugging sandboxed plug-ins.
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            //Ensure this plugin in running in the Pre Event stage of the Execution Pipeline
            if (context.Stage != 20)
                return;

            // The InputParameters collection contains all the data passed in the message request. For Plugins Registered to Delete Messages, Type will be 'EntityReference' NOT 'Entity'
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];

                // Verify that the target entity represents your target entity type.
                // If not, this plug-in was not registered correctly.
                if (entity.LogicalName != "account") //i.e. "account"
                    return;

                try
                {

                    // Obtain the organization service reference.
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    //This OrganizationService uses the security context of the user who triggered the plugin (saved / deleted a record etc)
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    //This Organization Service Impersonates the System Administrator
                    IOrganizationService impersonatedService = serviceFactory.CreateOrganizationService(null);

                    // Do Something in Microsoft Dynamics CRM.
                    tracingService.Trace("SamplePlugin: Doing Something.");

                    //Switch on Passed in Message Name

                    switch (context.MessageName.ToLower())
                    {
                        case "create":
                            //Update the Description to be the Name and the User ID
                            UpdateEntityDescription(entity, context.UserId);
                            break;
                        case "update":
                            //Do Nothing
                            break;
                        default:
                            break;
                    }                   

                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in the SamplePlugin plug-in.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("SamplePlugin: Exception Message {0}", ex.ToString());
                    throw;
                }
            }
        }

        private void UpdateEntityDescription(Entity entity, Guid userId)
        {
            //If this occurs in a Pre-Event Plugin Step, you can directly update / add attributes and their values to the context entity
            if (entity.Attributes.Contains("name"))
            {
                entity.Attributes["description"] = string.Format("Account: {0} - Created by User Id: {1}", entity.Attributes["name"], userId.ToString());
            }
        }
    }
}
