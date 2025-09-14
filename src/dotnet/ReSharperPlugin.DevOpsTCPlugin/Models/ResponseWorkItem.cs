namespace ReSharperPlugin.DevOpsTCPlugin.Models;

internal class ResponseWorkItem
{
    public int id { get; set; }

    public int rev { get; set; }

    public Fields fields { get; set; }

    public string url { get; set; }
}