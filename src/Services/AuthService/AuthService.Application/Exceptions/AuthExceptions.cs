namespace AuthService.Application.Exceptions;

public class AuthValidationException : Exception
{
    public AuthValidationException(string message) : base(message) { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid phone number or password.") { }
}

public class AccountBlockedException : Exception
{
    public AccountBlockedException() : base("Account is blocked.") { }
}

public class DuplicatePhoneException : Exception
{
    public DuplicatePhoneException(string phone) : base($"Phone {phone} is already registered.") { }
}

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Refresh token is invalid or expired.") { }
}

public class EsiaNotConfiguredException : Exception
{
    public EsiaNotConfiguredException() : base("ESIA integration is not configured.") { }
}
