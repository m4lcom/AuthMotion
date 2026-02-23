namespace AuthMotion.Application.Exceptions;

public class UnauthorizedException(string message) : BaseException(message, 401) { }