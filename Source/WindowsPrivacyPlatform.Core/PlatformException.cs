namespace WindowsPrivacyPlatform.Core;

public class PlatformException : Exception
{
    public PlatformException()
    {
    }

    public PlatformException(string message)
        : base(message)
    {
    }

    public PlatformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
