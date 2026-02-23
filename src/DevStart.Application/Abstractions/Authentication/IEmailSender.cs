namespace DevStart.Application.Abstractions.Authentication
{
    public interface IEmailSender
    {
        Task SendVerification(string email, string token);
    }
}
