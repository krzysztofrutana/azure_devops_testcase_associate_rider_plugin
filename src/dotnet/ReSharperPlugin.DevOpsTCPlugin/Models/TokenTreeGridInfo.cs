using System.Collections.Generic;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.Rider.Model.UIAutomation;
using ReSharperPlugin.DevOpsTCPlugin.Settings;

namespace ReSharperPlugin.DevOpsTCPlugin.Models;

public class TokenTreeGridInfo
{
    public DevOpsToken Token { get; set; }
    public ListNodeProperties Properties { get; set; }
    public List<BeControl> Controls { get; set; }
}