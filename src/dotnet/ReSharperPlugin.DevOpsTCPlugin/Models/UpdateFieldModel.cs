namespace ReSharperPlugin.DevOpsTCPlugin.Models;

internal class UpdateFieldModel
{
    public string Op { get; set; }
    public string Path { get; set; }
    public string Value { get; set; }
}