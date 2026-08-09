using DevStart.Domain.Profiles;

namespace DevStart.Application.Profiles
{
    /// <summary>
    /// The expert and investor dashboards each edit a subset of the shared <see cref="Profile"/> — the
    /// same fields their GetById handlers read back by joining it. Centralising the write keeps those
    /// read/write pairs symmetric and keeps the cache invalidation from being forgotten, which is the
    /// easy part to miss.
    /// </summary>
    /// <remarks>
    /// A user can hold an expert profile and an investor profile at once, so each form must write only
    /// what it actually shows. Anything a form does not display is left untouched rather than written
    /// as null — otherwise saving one form silently clears the other form's fields.
    /// </remarks>
    internal static class ProfilePersonalDetails
    {
        /// <summary>
        /// The fields every sub-profile form shows. Also marks the profile updated, so every caller
        /// gets the cache invalidation whether or not it writes anything else.
        /// </summary>
        public static void ApplyCore(
            Profile profile,
            string displayName,
            string? bio,
            string? website,
            bool isPublic)
        {
            profile.Name = displayName.Trim();
            profile.Bio = Normalize(bio);
            profile.Url = Normalize(website);
            profile.IsPublic = isPublic;

            // Profile and the aggregated user overview are cached; without this the dashboard would
            // keep serving the pre-save values.
            profile.Raise(new ProfileUpdatedDomainEvent(profile.UserId));
        }

        /// <summary>
        /// Only the expert form shows these four. The investor form must not call this — it would
        /// clear links the expert form owns.
        /// </summary>
        public static void ApplySocialLinks(
            Profile profile,
            string? linkedInUrl,
            string? twitterUrl,
            string? gitHubUrl,
            string? telegramUrl)
        {
            profile.LinkedInUrl = Normalize(linkedInUrl);
            profile.TwitterUrl = Normalize(twitterUrl);
            profile.GitHubUrl = Normalize(gitHubUrl);
            profile.TelegramUrl = Normalize(telegramUrl);
        }

        /// <summary>A cleared field arrives as an omitted/blank value and is stored as null, not "".</summary>
        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
