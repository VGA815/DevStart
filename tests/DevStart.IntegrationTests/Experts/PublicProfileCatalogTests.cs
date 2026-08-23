using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.Experts;
using DevStart.Domain.Investors;
using DevStart.Domain.Profiles;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Experts
{
    /// <summary>
    /// Каталоги инвесторов и экспертов открыты анониму, а карточки за ними — были закрыты правом на
    /// чтение. Наружу это выглядело как «инвестора/эксперта не существует» при клике по профилю,
    /// который посетитель только что видел в списке. Проверяется именно на HTTP-уровне: сломано было
    /// не то, что считает обработчик, а то, кого до него пускают.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class PublicProfileCatalogTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private sealed record CatalogRowDto(Guid UserId);
        private sealed record ProfileDto(Guid UserId, string DisplayName, bool IsPublic);
        private sealed record ExperienceDto(Guid Id, string Company);

        [Fact]
        public async Task AnonymousViewer_CanOpenAnInvestorCardListedInTheCatalog()
        {
            User investor = await SeedUserAsync();
            await SeedInvestorProfileAsync(investor.Id);

            HttpClient anonymous = CreateClient();

            List<CatalogRowDto>? catalog = await anonymous
                .GetFromJsonAsync<List<CatalogRowDto>>("api/investor-profiles?page=1&pageSize=20");
            catalog.ShouldNotBeNull();
            catalog.Select(r => r.UserId).ShouldContain(investor.Id);

            HttpResponseMessage card = await anonymous.GetAsync($"api/investor-profiles/{investor.Id}");
            card.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await card.Content.ReadFromJsonAsync<ProfileDto>())!.UserId.ShouldBe(investor.Id);
        }

        [Fact]
        public async Task AnonymousViewer_CanOpenAnExpertCardListedInTheCatalog_WithItsExperience()
        {
            User expert = await SeedUserAsync();
            await SeedExpertProfileAsync(expert.Id);
            await SeedExperienceAsync(expert.Id);

            HttpClient anonymous = CreateClient();

            List<CatalogRowDto>? catalog = await anonymous
                .GetFromJsonAsync<List<CatalogRowDto>>("api/expert-profiles?page=1&pageSize=20");
            catalog.ShouldNotBeNull();
            catalog.Select(r => r.UserId).ShouldContain(expert.Id);

            HttpResponseMessage card = await anonymous.GetAsync($"api/expert-profiles/{expert.Id}");
            card.StatusCode.ShouldBe(HttpStatusCode.OK);

            // Опыт — основное содержимое карточки: открывшаяся, но пустая страница ничем не лучше
            // «эксперта не существует».
            List<ExperienceDto>? experiences = await anonymous
                .GetFromJsonAsync<List<ExperienceDto>>($"api/expert-profiles/{expert.Id}/experiences");
            experiences.ShouldNotBeNull();
            experiences.Single().Company.ShouldBe("Acme");
        }

        [Fact]
        public async Task ANonPublicCard_StaysHiddenFromEveryoneButItsOwner()
        {
            User investor = await SeedUserAsync();
            User expert = await SeedUserAsync();
            User stranger = await SeedUserAsync();
            await SeedInvestorProfileAsync(investor.Id, isPublic: false);
            await SeedExpertProfileAsync(expert.Id, isPublic: false);
            await SeedExperienceAsync(expert.Id);

            HttpClient anonymous = CreateClient();
            HttpClient outsider = CreateAuthenticatedClient(stranger);

            // Каталог их не показывает — значит и прямая ссылка не подтверждает, что они есть.
            (await anonymous.GetAsync($"api/investor-profiles/{investor.Id}")).StatusCode
                .ShouldBe(HttpStatusCode.NotFound);
            (await outsider.GetAsync($"api/investor-profiles/{investor.Id}")).StatusCode
                .ShouldBe(HttpStatusCode.NotFound);
            (await anonymous.GetAsync($"api/expert-profiles/{expert.Id}")).StatusCode
                .ShouldBe(HttpStatusCode.NotFound);
            (await outsider.GetAsync($"api/expert-profiles/{expert.Id}")).StatusCode
                .ShouldBe(HttpStatusCode.NotFound);

            List<ExperienceDto>? hidden = await outsider
                .GetFromJsonAsync<List<ExperienceDto>>($"api/expert-profiles/{expert.Id}/experiences");
            hidden.ShouldNotBeNull();
            hidden.ShouldBeEmpty();

            // Владелец правит свою ещё не опубликованную карточку через те же эндпоинты — дашборд
            // читает именно их.
            HttpResponseMessage ownInvestorCard = await CreateAuthenticatedClient(investor)
                .GetAsync($"api/investor-profiles/{investor.Id}");
            ownInvestorCard.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ownInvestorCard.Content.ReadFromJsonAsync<ProfileDto>())!.IsPublic.ShouldBeFalse();

            HttpClient owner = CreateAuthenticatedClient(expert);
            (await owner.GetAsync($"api/expert-profiles/{expert.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

            List<ExperienceDto>? own = await owner
                .GetFromJsonAsync<List<ExperienceDto>>($"api/expert-profiles/{expert.Id}/experiences");
            own.ShouldNotBeNull();
            own.Single().Company.ShouldBe("Acme");
        }

        private Task SeedInvestorProfileAsync(Guid userId, bool isPublic = true) => ExecuteDbAsync(async db =>
        {
            db.InvestorProfiles.Add(InvestorProfile.Create(userId, InvestorProfileType.Fund, DateTime.UtcNow));
            await SetVisibilityAsync(db, userId, isPublic);
        });

        private Task SeedExpertProfileAsync(Guid userId, bool isPublic = true) => ExecuteDbAsync(async db =>
        {
            db.ExpertProfiles.Add(ExpertProfile.Create(userId, DateTime.UtcNow));
            await SetVisibilityAsync(db, userId, isPublic);
        });

        /// <summary>Профиль эксперта заводится с Id == UserId, поэтому опыт крепится по нему же.</summary>
        private Task SeedExperienceAsync(Guid expertProfileId) => ExecuteDbAsync(async db =>
        {
            db.ExpertExperiences.Add(ExpertExperience.Create(
                expertProfileId, "Acme", "CPO", new DateOnly(2020, 1, 1), null, null, DateTime.UtcNow));
            await db.SaveChangesAsync();
        });

        private static async Task SetVisibilityAsync(ApplicationDbContext db, Guid userId, bool isPublic)
        {
            Profile profile = await db.Profiles.SingleAsync(p => p.UserId == userId);
            profile.IsPublic = isPublic;
            await db.SaveChangesAsync();
        }
    }
}
