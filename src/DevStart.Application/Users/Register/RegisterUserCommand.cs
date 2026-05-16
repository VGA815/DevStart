using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserConsents;

namespace DevStart.Application.Users.Register
{
    public sealed class RegisterUserCommand : ICommand<Guid>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? Bio { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public bool IsPublic { get; set; }
        public List<ConsentItem> Consents { get; set; } = [];

        public RegisterUserCommand(
            string email,
            string password,
            string username,
            string? bio,
            string? name,
            string? url,
            List<string> socialMediaLinks,
            bool isPublic,
            List<ConsentItem> consents)
        {
            Email            = email;
            Password         = password;
            Username         = username;
            Bio              = bio;
            Name             = name;
            Url              = url;
            SocialMediaLinks = socialMediaLinks;
            IsPublic         = isPublic;
            Consents         = consents;
        }
    }

    public sealed record ConsentItem(ConsentType Type, string DocumentVersion, bool Accepted);
}
