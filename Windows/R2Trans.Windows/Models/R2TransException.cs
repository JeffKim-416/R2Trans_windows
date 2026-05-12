namespace R2Trans.Windows.Models;

public sealed class R2TransException : Exception
{
    public R2TransException(string message)
        : base(message)
    {
    }

    public R2TransException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
