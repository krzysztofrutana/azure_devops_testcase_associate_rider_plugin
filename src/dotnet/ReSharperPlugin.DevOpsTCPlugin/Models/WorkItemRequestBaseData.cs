using System;
using System.Collections.Generic;
using System.Net.Http;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ReSharperPlugin.DevOpsTCPlugin.Settings;

namespace ReSharperPlugin.DevOpsTCPlugin.Models;

internal class WorkItemRequestBaseData
{
    public WorkItemRequestBaseData([NotNull] DevOpsToken token)
    {
        if (token == null) throw new Exception(Resources.TokenIsRequired);
        
        Organization = token.Organization;
        OrganizationProject = token.OrganizationProject;
        Pat = token.Pat;
    }
    
    private const string AzureTestPathField = "/fields/Microsoft.VSTS.TCM.AutomatedTestName";
    private const string AzureTestDllField = "/fields/Microsoft.VSTS.TCM.AutomatedTestStorage";
    private const string AzureTestIdField = "/fields/Microsoft.VSTS.TCM.AutomatedTestId";
    private const string AzureTestCaseAutomationStatus = "/fields/Microsoft.VSTS.TCM.AutomationStatus";
    private const string AzureTestCaseAutomatedTestType = "/fields/Microsoft.VSTS.TCM.AutomatedTestType";
    
    public string Organization { get; init; }
    public string OrganizationProject { get; init; }
    public string TestCaseId { get; set; }
    public string TestPath { get; set; }
    public string ProjectDll { get; set; }
    public string Pat { get; init; }
    public string TestType { get; set; }
    
    public string UpdateUrl => 
         $"https://dev.azure.com/{Organization}/{OrganizationProject}/_apis/wit/workitems/{TestCaseId}?api-version=7.1";

    public string GetUrl =>
        $"https://dev.azure.com/{Organization}/{OrganizationProject}/_apis/wit/workitems/{TestCaseId}?api-version=7.1";
    
    public string GetAssignedUrl =>
        $"https://dev.azure.com/{Organization}/{OrganizationProject}/_apis/wit/wiql?api-version=7.1";
    
    public string PatBase64 
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($":{Pat}"));

    public HttpRequestMessage PrepareGetRequestMessage()
    {
        var request = new HttpRequestMessage(new HttpMethod("GET"), GetUrl);
        request.Headers.Add("Authorization", $"Basic {PatBase64}");

        return request;
    }
    
    public HttpRequestMessage PreparePatchRequestMessage(Guid azureTestIdGuid)
    {
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), UpdateUrl);
        request.Headers.Add("Authorization", $"Basic {PatBase64}");
        request.Content = new StringContent(JsonConvert.SerializeObject(GetUpdateArray(azureTestIdGuid)), System.Text.Encoding.UTF8, "application/json-patch+json");
        
        return request;   
    }
    
    public HttpRequestMessage PrepareGetAssignedRequestMessage()
    {
        var request = new HttpRequestMessage(new HttpMethod("POST"), GetAssignedUrl);
        request.Headers.Add("Authorization", $"Basic {PatBase64}");

        var query = new
        {
            query = $@"SELECT [System.Id] 
                        FROM WorkItems 
                        WHERE [System.TeamProject] = @project 
                        AND [System.WorkItemType] = 'Test Case' 
                        AND [Microsoft.VSTS.TCM.AutomatedTestName] = '{TestPath}'
                        AND [Microsoft.VSTS.TCM.AutomatedTestStorage] = '{ProjectDll}'"
        };
        request.Content = new StringContent(JsonConvert.SerializeObject(query), System.Text.Encoding.UTF8, "application/json");
        
        return request;
    }

    public UpdateFieldModel[] GetUpdateArray(Guid azureTestIdGuid)
    {
        var list = new List<UpdateFieldModel>()
        {
            new UpdateFieldModel()
            {
                Op = "add",
                Path = AzureTestPathField,
                Value = TestPath
            },
            new UpdateFieldModel()
            {
                Op = "add",
                Path = AzureTestDllField,
                Value = ProjectDll
            },
            new UpdateFieldModel()
            {
                Op = "add",
                Path = AzureTestIdField,
                Value = azureTestIdGuid.ToString()
            },
            new UpdateFieldModel()
            {
                Op = "add",
                Path = AzureTestCaseAutomationStatus,
                Value = "Automated"
            }
        };

        if (!string.IsNullOrEmpty(TestType))
        {
            list.Add(new UpdateFieldModel()
            {
                Op = "add",
                Path = AzureTestCaseAutomatedTestType,
                Value = TestType
            });
        }

        return list.ToArray();
    } 
    
    public bool Validate(bool onlyGetAlreadyAssigned, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(Pat))
        {
            errorMessage = Resources.NoPATMessage;
            return false;
        }
        if (string.IsNullOrWhiteSpace(Organization))
        {
            errorMessage = Resources.OrganizationCannotBeEmpty;
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(OrganizationProject))
        {
            errorMessage = Resources.ProjectDllCannotBeEmpty;
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(TestPath))
        {
            errorMessage = Resources.TestPathCannotBeEmpty;
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(ProjectDll))
        {
            errorMessage = Resources.ProjectDllCannotBeEmpty;
            return false;
        }

        if (!onlyGetAlreadyAssigned)
        {
            if (string.IsNullOrWhiteSpace(TestCaseId))
            {
                errorMessage = Resources.TestCaseIdCannotBeEmpty;
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    public bool CheckWorkItemMusBeUpdated(ResponseWorkItem workItem, out Guid azureTestIdGuid)
    {
        bool mustUpdateWorkItem = false;
        
        if (!string.IsNullOrEmpty(workItem.fields.MicrosoftVSTSTCMAutomatedTestId))
        {
            azureTestIdGuid = Guid.Parse(workItem.fields.MicrosoftVSTSTCMAutomatedTestId);
        }
        else
        {
            azureTestIdGuid = Guid.NewGuid();
            mustUpdateWorkItem = true;
        }
                
        if (TestPath != workItem.fields.MicrosoftVSTSTCMAutomatedTestName)
        {
            mustUpdateWorkItem = true;
        }

        if (ProjectDll != workItem.fields.MicrosoftVSTSTCMAutomatedTestStorage)
        {
            mustUpdateWorkItem = true;
        }
        
        if (TestType != workItem.fields.MicrosoftVSTSTCMAutomationTestType)
        {
            mustUpdateWorkItem = true;
        }
        
        return mustUpdateWorkItem;
    }
}