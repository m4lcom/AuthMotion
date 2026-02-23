namespace AuthMotion.Application.Exceptions;

public abstract class BaseException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}