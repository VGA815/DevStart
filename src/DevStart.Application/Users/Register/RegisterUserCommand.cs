using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.Register
{
    public sealed class RegisterUserCommand: ICommand<Guid>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? Bio { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public bool IsPublic { get; set; }
    }
}
