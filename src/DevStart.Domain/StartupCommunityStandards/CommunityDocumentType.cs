namespace DevStart.Domain.StartupCommunityStandards
{
    /// <summary>
    /// The community health documents a startup can publish. One document per type per startup.
    /// </summary>
    public enum CommunityDocumentType
    {
        CodeOfConduct  = 0,
        Contributing   = 1,
        Support        = 2,
        SecurityPolicy = 3,
        Legal          = 4
    }
}
