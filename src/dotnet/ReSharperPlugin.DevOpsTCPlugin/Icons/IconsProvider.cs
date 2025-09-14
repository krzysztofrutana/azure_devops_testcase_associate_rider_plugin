using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.IDE.UI;
using JetBrains.ProjectModel;
using JetBrains.Rider.Model;
using JetBrains.UI.Icons;

namespace ReSharperPlugin.DevOpsTCPlugin.Icons;

[ShellComponent]
public class IconsProvider
{
    private readonly IIconHost _iconHost;

    public IconsProvider(IIconHost iconHost)
    {
        _iconHost = iconHost;
    }

    public IconModel GetIcon(IconId iconId)
        => _iconHost.Transform(iconId);
}