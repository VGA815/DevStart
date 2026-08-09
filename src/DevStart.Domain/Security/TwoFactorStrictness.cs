namespace DevStart.Domain.Security
{
    /// <summary>
    /// How aggressively the second factor is demanded once 2FA is enabled. Chosen by the user;
    /// evaluated by the login gate before any trusted-device token is read.
    /// </summary>
    public enum TwoFactorStrictness
    {
        /// <summary>
        /// A code on every single login. Trusted-device tokens are not consulted at all, and
        /// selecting this level revokes the devices the user already trusted.
        /// </summary>
        EveryLogin = 0,

        /// <summary>Default. A trusted device skips the challenge until its token expires.</summary>
        RememberDevice = 1,

        /// <summary>
        /// As <see cref="RememberDevice"/>, but the current IP must still share a subnet with the one
        /// the device was trusted from. A mismatch re-challenges without revoking the device — the
        /// user may simply be somewhere else today.
        /// </summary>
        SameNetworkOnly = 2,
    }
}
