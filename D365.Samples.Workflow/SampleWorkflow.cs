using System;
using System.Activities;
using System.Collections.ObjectModel;

using Microsoft.Crm.Sdk.Messages;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace D365.Samples.Workflow
{

    public sealed class SimpleSdkActivity : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            //Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            //Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("Resolving Account from Context");

            Account entity = new Account();
            entity.Id = ((EntityReference)AccountReference.Get<EntityReference>(executionContext)).Id;

            tracingService.Trace("Account resolved with Id {0}", entity.Id.ToString());

            tracingService.Trace("Create a note for the account");
            Annotation newNote = new Annotation();
            newNote.Subject = NoteSubject.Get<string>(executionContext);
            newNote.NoteText = NoteBody.Get<string>(executionContext);
            newNote.ObjectId = entity.ToEntityReference();

            Guid noteId = service.Create(newNote);

            tracingService.Trace("Note has been created");

            NoteReference.Set(executionContext, new EntityReference(Annotation.EntityLogicalName, noteId));

        }

        [Input("Account")]
        [RequiredArgument]
        public InArgument<EntityReference> AccountReference { get; set; }

        [Input("Note Subject")]
        [Default("")]
        public InArgument<string> NoteSubject { get; set; }

        [Input("Note Body")]
        [Default("")]
        public InArgument<string> NoteBody { get; set; }

        [Output("Note Id")]
        public OutArgument<EntityReference> NoteReference { get; set; }
    }
}
