using System.Text;

namespace ReSharperPlugin.DevOpsTCPlugin;

public static class Helpers
{
    public static string HidePat(string pat)
    {
        if (string.IsNullOrWhiteSpace(pat))
            return string.Empty;

        var sb = new StringBuilder();
        var index = 0;
        foreach (var sign in pat)
        {
            if (index > 4)
                sb.Append("*");
            else
                sb.Append(sign);

            index++;
        }
        
        return sb.ToString();
    }
}