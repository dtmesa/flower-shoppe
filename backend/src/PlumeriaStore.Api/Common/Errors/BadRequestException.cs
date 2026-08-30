namespace PlumeriaStore.Api.Common.Errors;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}
