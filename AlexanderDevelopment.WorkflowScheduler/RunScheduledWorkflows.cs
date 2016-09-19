using System;
using System.Activities;
using System.ServiceModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using System.Net;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace AlexanderDevelopment.WorkflowScheduler
{
    public sealed class RunScheduledWorkflows : CodeActivity
    {
        [RequiredArgument]
        [Input("FetchXML query")]
        public InArgument<String> FetchXMLQuery { get; set; }

        [RequiredArgument]
        [Input("Workflow")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Workflow { get; set; }

        [RequiredArgument]
        [Input("Page size")]
        public InArgument<Int32> PageSize { get; set; }

        //name of your custom workflow activity for tracing/error logging
        private string _activityName = "RunRecurringProcess";

        /// <summary>
        /// Executes the workflow activity.
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("Entered " + _activityName + ".Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace(_activityName + ".Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            
            try
            {
                //create empty entitycollection for later
                EntityCollection recordsToProcess = new EntityCollection();

                // set page size
                int fetchCount = PageSize.Get(executionContext);

                // Initialize the page number.
                int pageNumber = 1;

                // Specify the current paging cookie. For retrieving the first page, 
                // pagingCookie should be null.
                string pagingCookie = null;

                //execute query and page through results
                while (true)
                {
                    // Build fetchXml string with the placeholders.
                    string fetchXml = CreateXml(FetchXMLQuery.Get(executionContext), pagingCookie, pageNumber, fetchCount);

                    EntityCollection retrieved = service.RetrieveMultiple(new FetchExpression(fetchXml));

                    //add retrieved records to our main entitycollection
                    recordsToProcess.Entities.AddRange(retrieved.Entities);

                    if (retrieved.MoreRecords)
                    {
                        // Increment the page number to retrieve the next page.
                        pageNumber++;

                        // Set the paging cookie to the paging cookie returned from current results.                            
                        pagingCookie = retrieved.PagingCookie;
                    }
                    else
                    {
                        // If no more records in the result nodes, exit the loop.
                        break;
                    }
                }

                //loop through results
                recordsToProcess.Entities.ToList().ForEach(a =>
                {
                    ExecuteWorkflowRequest request = new ExecuteWorkflowRequest
                    {
                        EntityId = a.Id,
                        WorkflowId = (Workflow.Get(executionContext)).Id
                    };

                    service.Execute(request);  //run the workflow
                });


            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());

                // Handle the exception.
                throw;
            }
            catch (Exception e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());
                throw;
            }

            tracingService.Trace("Exiting " + _activityName + ".Execute(), Correlation Id: {0}", context.CorrelationId);
        }
    
        /// <summary>
        /// used to enable paging in the fetchxml queries - https://msdn.microsoft.com/en-us/library/gg328046.aspx
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="cookie"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader stringReader = new StringReader(xml);
            XmlTextReader reader = new XmlTextReader(stringReader);

            // Load document
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            return CreateXml(doc, cookie, page, count);
        }

        /// <summary>
        /// used to enable paging in the fetchxml queries - https://msdn.microsoft.com/en-us/library/gg328046.aspx
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="cookie"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            XmlAttribute pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = System.Convert.ToString(page);
            attrs.Append(pageAttr);

            XmlAttribute countAttr = doc.CreateAttribute("count");
            countAttr.Value = System.Convert.ToString(count);
            attrs.Append(countAttr);

            StringBuilder sb = new StringBuilder(1024);
            StringWriter stringWriter = new StringWriter(sb);

            XmlTextWriter writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }
    }
}