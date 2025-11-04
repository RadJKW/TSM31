namespace TSM31.WinForm.Services;

using Core.Services.Config;

public class FormFactor : IFormFactor
{
    public string GetFormFactor()
    {
        // DeviceInfo is maui specific API
        return "Desktop";
    }

    public string GetPlatform()
    {
        // get the Windows os info using System.Environment
        return Environment.OSVersion.Platform.ToString();
    }
}
