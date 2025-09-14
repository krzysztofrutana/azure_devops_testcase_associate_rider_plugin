using Newtonsoft.Json;

namespace ReSharperPlugin.DevOpsTCPlugin.Models;

public class Fields
{
    [JsonProperty("Microsoft.VSTS.TCM.AutomatedTestName")]
    public string MicrosoftVSTSTCMAutomatedTestName { get; set; }

    [JsonProperty("Microsoft.VSTS.TCM.AutomatedTestStorage")]
    public string MicrosoftVSTSTCMAutomatedTestStorage { get; set; }

    [JsonProperty("Microsoft.VSTS.TCM.AutomatedTestId")]
    public string MicrosoftVSTSTCMAutomatedTestId { get; set; }

    [JsonProperty("Microsoft.VSTS.TCM.AutomationStatus")]
    public string MicrosoftVSTSTCMAutomationStatus { get; set; }
    
    [JsonProperty("Microsoft.VSTS.TCM.AutomatedTestType")]
    public string MicrosoftVSTSTCMAutomationTestType { get; set; }
    
    [JsonProperty("System.Title")]
    public string SystemTitle { get; set; }
}