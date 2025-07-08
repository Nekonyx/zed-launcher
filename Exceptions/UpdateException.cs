namespace ZedLauncher.Exceptions;

public sealed class UpdateException : Exception
{
    public UpdateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}