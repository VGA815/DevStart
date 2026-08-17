namespace DevStart.Domain.Admin
{
    public enum AdminActionType
    {
        BanUser = 0,
        UnbanUser = 1,
        BanStartup = 2,
        UnbanStartup = 3,
        GrantSubscription = 4,
        ExtendSubscription = 5,
        RevokeSubscription = 6,
        CreatePromoCode = 7,
        DeactivatePromoCode = 8,
        AddValuationBenchmark = 9,
        ResetUserTwoFactor = 10,
        CancelServiceOrder = 11,
        SaveBenchmarkIssuer = 12,
        SaveBenchmarkIndustryMapping = 13,
        DeleteBenchmarkIndustryMapping = 14,
        UploadDamodaranDataset = 15,
        RunBenchmarkCollection = 16,
    }
}
