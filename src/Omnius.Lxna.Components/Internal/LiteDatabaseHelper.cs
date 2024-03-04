namespace Omnius.Lxna.Components.Internal;

public static class LiteDatabaseHelper
{
    public static string GetConnectionString(string filePath)
    {
        var encryptPassword = Environment.GetEnvironmentVariable("LXNA_PASSWORD");

        if (encryptPassword is null)
        {
            return $"Filename=\"{filePath}\"";
        }
        else
        {
            return $"Filename=\"{filePath}\";Password=\"{encryptPassword}\"";
        }
    }
}
