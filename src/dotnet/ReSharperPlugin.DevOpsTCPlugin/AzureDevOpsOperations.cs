using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Newtonsoft.Json;
using ReSharperPlugin.DevOpsTCPlugin.Models;
using ReSharperPlugin.DevOpsTCPlugin.Settings;

namespace ReSharperPlugin.DevOpsTCPlugin;

public static class AzureDevOpsOperations
{
    internal static List<WorkItemInfo> GetAlreadyAssignedItems(DevOpsToken selectedToken, string testPath, string projectDll)
    {
        var result = new List<WorkItemInfo>();
        
        if (selectedToken is not null)
        {
            try
            {
                var model = new WorkItemRequestBaseData(selectedToken)
                {
                    TestPath = testPath,
                    ProjectDll = projectDll,
                };

                if (model.Validate(true, out _))
                    result = GetAssignedWorkItems(model);
            }
            catch
            {
                // ignored
            }
        }

        return result;
    }
    
    internal static HttpResponseMessage UpdateWorkitem(
        HttpClient httpClient, 
        WorkItemRequestBaseData requestBaseData, 
        Guid azureTestIdGuid)
    {
        var result = httpClient.SendAsync(requestBaseData.PreparePatchRequestMessage(azureTestIdGuid)).GetAwaiter().GetResult();
        return result;
    }

    internal static ResponseWorkItem GetWorkItem(WorkItemRequestBaseData requestBaseData)
    {
        using (var httpClient = new HttpClient())
        {
            var response = httpClient.SendAsync(requestBaseData.PrepareGetRequestMessage()).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            ResponseWorkItem data = JsonConvert.DeserializeObject<ResponseWorkItem>(responseBody);
        
            return data;
        }
    }

    internal static ResponseWorkItem GetWorkitem(HttpClient httpClient, WorkItemRequestBaseData requestBaseData)
    {
        var response = httpClient.SendAsync(requestBaseData.PrepareGetRequestMessage()).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        
        var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        ResponseWorkItem data = JsonConvert.DeserializeObject<ResponseWorkItem>(responseBody);
        
        return data;
    }
    
    internal static List<WorkItemInfo> GetAssignedWorkItems(WorkItemRequestBaseData requestBaseData)
    {
        using (var httpClient = new HttpClient())
        {
            var response = httpClient.SendAsync(requestBaseData.PrepareGetAssignedRequestMessage()).GetAwaiter().GetResult();
            
            if(!response.IsSuccessStatusCode)
                return new List<WorkItemInfo>();
            
            string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                
            if (!data.TryGetProperty("workItems", out var workItems))
                return new List<WorkItemInfo>();

            var testIds = workItems.EnumerateArray().Select(w => w.GetProperty("id").GetInt32()).ToList();

            var result = new List<WorkItemInfo>();
            
            foreach (var id in testIds)
            {
                requestBaseData.TestCaseId = id.ToString();
                var workItem = GetWorkitem(httpClient, requestBaseData);
                if (workItem != null)
                {
                    result.Add(new WorkItemInfo()
                    {
                        Id = id,
                        Name = workItem.fields.SystemTitle
                    });
                }
            }
            return result;
        }
    }
}