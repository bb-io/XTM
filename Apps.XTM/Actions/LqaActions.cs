using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Response;
using Apps.XTM.Models.Response.Customers;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using DocumentFormat.OpenXml.Bibliography;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Actions
{
    [ActionList]
    public class LqaActions : XtmInvocable
    {
        public LqaActions(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        [Action("Search LQA reports", Description = "Define criteria to search LQA reports")]
        public async Task<List<LqaResponse>> SearchLqa(LQARequest input)
        {
            var Params = new List<string>();
            if (input.DateFrom is not null) Params.Add($"completeDateFrom={input.DateFrom:yyyy-MM-dd}");
            if (input.DateTo is not null) Params.Add($"completeDateTo={input.DateTo:yyyy-MM-dd}");
            if (input.TargetLangs is not null) Params.Add($"targetLanguages={String.Join(",",input.TargetLangs)}");
            if (input.Type is not null) Params.Add($"type={input.Type}");
            
            var endpoint = $"{ApiEndpoints.Projects}/lqa/download";
            if (Params is not null && Params.Count > 0)
            { endpoint = endpoint + "?"+String.Join("&",Params); }
            
            var response = await Client.ExecuteXtmWithJson<List<LqaResponse>>(endpoint,
                Method.Get,
            null,
            Creds);

            return FixDate(response);
        }

        private List<LqaResponse> FixDate(List<LqaResponse> response)
        {
            var updated = new List<LqaResponse>();
            foreach (var item in response) 
            {

                updated.Add(new LqaResponse 
                {
                    id = item.id,
                    evaluator = item.evaluator,
                    project = item.project,
                    customer = item.customer,
                    language = item.language,
                    evaluee = item.evaluee,
                    files = item.files,
                    severityMultipliers = item.severityMultipliers,
                    completeDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(item.completeDate)).ToString("yyyy-MM-dd")
                });
            }

            return updated;
        }
    }
}
