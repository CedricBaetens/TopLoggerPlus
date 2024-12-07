namespace TopLoggerPlus.Contracts.Utils;

public abstract class TopLoggerPlusException : Exception
{
    protected TopLoggerPlusException(string message) : base(message)
    {
    }
    protected TopLoggerPlusException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class GraphQLFailedException : TopLoggerPlusException
{
    public GraphQLFailedException(string message) : base(message)
    {
    }
    public GraphQLFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
public class AuthenticationFailedException : TopLoggerPlusException
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
    public AuthenticationFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}