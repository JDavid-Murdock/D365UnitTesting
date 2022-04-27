using System;
using System.Activities;
using System.Collections.Generic;
using System.Configuration;      
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Tooling.Connector;
using Moq;

namespace D365.Samples.Workflow.Tests
{
    [TestClass]
    public class SampleWorkflowTests
    {

        #region Class Level Members

        private IOrganizationService _orgService;
        private IOrganizationService _mockedOrgService;

        #endregion

        #region Constants

        string testNoteSubject = "Test Note Subject";
        string testNoteBody = "Test Note Body";

        #endregion

        [TestMethod]
        public void SimpleSdkActivityTestMethod()
        {
            Entity target = null;

            Guid testAccountId = new Guid("475B158C-541C-E511-80D3-3863BB347BA8"); //AVAOttBootCamp: 475B158C-541C-E511-80D3-3863BB347BA8 DMJuly2018:0A2A9CD9-C186-E811-A960-000D3AF4AD22

            //Connection string to CRM
            string connectionString = ConfigurationManager.ConnectionStrings["AVAOttBootCamp"].ConnectionString;

            //Connect to CRM
            CrmServiceClient conn = new CrmServiceClient(connectionString);

            // Cast the proxy client to the IOrganizationService interface.
            _orgService = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;


            try
            {
                //Retrieve an actual record to serve as the Target
                target = _orgService.Retrieve("account", testAccountId, new Microsoft.Xrm.Sdk.Query.ColumnSet("accountid"));

                //Initialize an instance of the Workflow Method Object
                var simpleSdkActivity = new SimpleSdkActivity();

                //Instantiate a Workflow Invoker
                var invoker = new WorkflowInvoker(simpleSdkActivity);

                //create our mocks
                var serviceMock = new Mock<IOrganizationService>();
                var factoryMock = new Mock<IOrganizationServiceFactory>();
                var tracingServiceMock = new Mock<ITracingService>();
                var workflowContextMock = new Mock<IWorkflowContext>();

                //set up a mock service to act like the CRM organization service
                _mockedOrgService = serviceMock.Object;

                //set up a mock workflowcontext
                var workflowUserId = Guid.NewGuid();
                var workflowCorrelationId = Guid.NewGuid();
                var workflowInitiatingUserId = Guid.NewGuid();
                workflowContextMock.Setup(t => t.InitiatingUserId).Returns(workflowInitiatingUserId);
                workflowContextMock.Setup(t => t.CorrelationId).Returns(workflowCorrelationId);
                workflowContextMock.Setup(t => t.UserId).Returns(workflowUserId);
                var workflowContext = workflowContextMock.Object;

                //set up a mock tracingservice - will write output to console
                tracingServiceMock.Setup(t => t.Trace(It.IsAny<string>(), It.IsAny<object[]>())).Callback<string, object[]>((t1, t2) => Console.WriteLine(t1, t2));
                var tracingService = tracingServiceMock.Object;

                //set up a mock servicefactory, and have it return a real OrgService when it's 'CreateOrganizationService' method is called
                factoryMock.Setup(t => t.CreateOrganizationService(It.IsAny<Guid?>())).Returns(_orgService);
                var factory = factoryMock.Object;

                //Add extensions to the invoker
                invoker.Extensions.Add<ITracingService>(() => tracingService);
                invoker.Extensions.Add<IWorkflowContext>(() => workflowContext);
                invoker.Extensions.Add<IOrganizationServiceFactory>(() => factory);

                //Set the Workflow Input Parameter to a Reference Queried from CRM
                var inputs = new Dictionary<string, object>
            {
                {"AccountReference", target.ToEntityReference() },
                { "NoteSubject", testNoteSubject},
                { "NoteBody", testNoteBody}

            };

                ParameterCollection inputParameters = new ParameterCollection();
                inputParameters.Add("Target", target);

                workflowContextMock.Setup(t => t.InputParameters).Returns(inputParameters);

                var outputs = invoker.Invoke(inputs);

                //Test
                EntityReference createdNoteReference = (EntityReference)outputs["NoteReference"];

                if (createdNoteReference.Id == Guid.Empty)
                    Assert.Fail("Note Id Invalid");
                else

                #region Clean Up
                {

                    _orgService.Delete(createdNoteReference.LogicalName, createdNoteReference.Id);
                }
                #endregion

            }
            catch (Exception ex)
            {

            }
        }
    }
}
