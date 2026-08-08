using DevStart.Domain.StartupMembers;

namespace DevStart.Domain.Messages
{
    /// <summary>
    /// Who may speak — and listen — as a startup. Talking to outsiders on behalf of the company is a
    /// leadership act, so a plain <see cref="StartupRole.Member"/> neither sends nor reads the
    /// startup's conversations.
    /// </summary>
    public static class MessagingRoles
    {
        public static readonly StartupRole[] CanActAsStartup =
        [
            StartupRole.Founder,
            StartupRole.Administration,
        ];

        public static bool CanAct(StartupRole role) => CanActAsStartup.Contains(role);
    }
}
