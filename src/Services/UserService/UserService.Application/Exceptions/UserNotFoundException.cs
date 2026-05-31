namespace UserService.Application.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class DuplicatePhoneException : Exception
    {
        public DuplicatePhoneException(string message) : base(message) { }
    }

    public class DuplicateEmailException : Exception
    {
        public DuplicateEmailException(string message) : base(message) { }
    }
}