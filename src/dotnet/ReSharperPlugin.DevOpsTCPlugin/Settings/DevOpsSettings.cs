using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Communication;
using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using Newtonsoft.Json;

namespace ReSharperPlugin.DevOpsTCPlugin.Settings;

[SettingsKey( typeof(InternetSettings), "AzureDevOpsTokensConfiguration")]
public class DevOpsSettings
{
    [SettingsEntry("", "TokensJson")]
    public string TokensJson { get; set; }


    private DevOpsToken[] _tokens;
    public DevOpsToken[] Tokens
    {
        get
        {
            if (_tokens == null)
            {
                _tokens = JsonConvert.DeserializeObject<DevOpsToken[]>(TokensJson);
            }
            
            return _tokens;
        }
        set
        {
            _tokens = value;
            TokensJson = JsonConvert.SerializeObject(value);
        }
    }

    private ListEvents<DevOpsToken> _tokensListEvent;
    
    public ListEvents<DevOpsToken> TokensListEvent
    {
        get
        {
            if (_tokensListEvent == null)
            {
                _tokensListEvent = ListEvents<DevOpsToken>.Create("TokensListEvent");
                _tokensListEvent.AddRange(Tokens);
            }
            
            return _tokensListEvent;
        }
    }
}

public class DevOpsToken
{
    public string Organization { get; set; }
    public string OrganizationProject { get; set; }
    public string Pat { get; set; }
    public string SolutionName { get; set; }
    
    public bool SomethingMissing => string.IsNullOrWhiteSpace(Pat) 
                                    || string.IsNullOrWhiteSpace(Organization) 
                                    || string.IsNullOrWhiteSpace(OrganizationProject) 
                                    || string.IsNullOrWhiteSpace(SolutionName);

    public override string ToString()
    {
        return $"{SolutionName} - {Organization} ({OrganizationProject}): {string.Join("", Pat.Take(5)) + "..."}";
    }
}