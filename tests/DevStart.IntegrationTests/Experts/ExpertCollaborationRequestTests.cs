using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Experts;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Experts
{
    /// <summary>
    /// The two directions over HTTP. This is the layer where the client contract lives — the request
    /// body decides who is being invited, and the caller's role decides which way the request runs —
    /// so it is covered here rather than only at the handler level.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class ExpertCollaborationRequestTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private sealed record RequestDto(
            Guid Id,
            Guid ExpertProfileId,
            string ExpertDisplayName,
            Guid StartupId,
            string StartupName,
            int Initiator,
            int Status);

        /// <summary>CustomResults.Problem puts the error code in the ProblemDetails title.</summary>
        private sealed record ProblemDto(string? Title);

        [Fact]
        public async Task FounderCanInviteAnExpert_AndTheExpertAnswers()
        {
            User founder = await SeedUserAsync();
            User expert = await SeedUserAsync();
            await SeedExpertProfileAsync(expert.Id);
            Guid startupId = await SeedStartupAsync((founder.Id, StartupRole.Founder));

            HttpResponseMessage invite = await CreateAuthenticatedClient(founder)
                .PostAsJsonAsync("api/expert-collaboration-requests", new
                {
                    startup_id = startupId,
                    expert_profile_id = expert.Id,
                    collaboration_type = (int)CollaborationType.Advisor,
                    message = "Нужен эдвайзер по продукту",
                });

            invite.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid requestId = await invite.Content.ReadFromJsonAsync<Guid>();

            // The invitation is filed against the expert, not the founder who sent it.
            ExpertCollaborationRequest stored = await ExecuteDbAsync(db =>
                db.ExpertCollaborationRequests.SingleAsync(r => r.Id == requestId));
            stored.ExpertProfileId.ShouldBe(expert.Id);
            stored.Initiator.ShouldBe(CollaborationRequestInitiator.Startup);

            // It shows up in the expert's list, and the expert is the one who may accept it.
            List<RequestDto> expertInbox = await ListAsync(expert, $"api/expert-profiles/{expert.Id}/expert-collaboration-requests");
            expertInbox.Single().Id.ShouldBe(requestId);

            HttpResponseMessage founderAccept = await CreateAuthenticatedClient(founder)
                .PostAsJsonAsync($"api/expert-collaboration-requests/{requestId}/accept", new { });
            founderAccept.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            HttpResponseMessage expertAccept = await CreateAuthenticatedClient(expert)
                .PostAsJsonAsync($"api/expert-collaboration-requests/{requestId}/accept", new { });
            expertAccept.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ExpertCanApplyToAStartup_AndTheFounderAnswers()
        {
            User founder = await SeedUserAsync();
            User expert = await SeedUserAsync();
            await SeedExpertProfileAsync(expert.Id);
            Guid startupId = await SeedStartupAsync((founder.Id, StartupRole.Founder));

            HttpResponseMessage apply = await CreateAuthenticatedClient(expert)
                .PostAsJsonAsync("api/expert-collaboration-requests", new
                {
                    startup_id = startupId,
                    collaboration_type = (int)CollaborationType.Mentor,
                });

            apply.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid requestId = await apply.Content.ReadFromJsonAsync<Guid>();

            List<RequestDto> startupInbox = await ListAsync(founder, $"api/startups/{startupId}/expert-collaboration-requests");
            RequestDto row = startupInbox.Single();
            row.Id.ShouldBe(requestId);
            row.Initiator.ShouldBe((int)CollaborationRequestInitiator.Expert);
            row.ExpertDisplayName.ShouldNotBeNullOrEmpty();
            row.StartupName.ShouldNotBeNullOrEmpty();

            HttpResponseMessage expertAccept = await CreateAuthenticatedClient(expert)
                .PostAsJsonAsync($"api/expert-collaboration-requests/{requestId}/accept", new { });
            expertAccept.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            HttpResponseMessage founderReject = await CreateAuthenticatedClient(founder)
                .PostAsJsonAsync($"api/expert-collaboration-requests/{requestId}/reject", new { });
            founderReject.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task InvitingWithoutNamingTheExpertIsRejected()
        {
            User founder = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync((founder.Id, StartupRole.Founder));

            HttpResponseMessage invite = await CreateAuthenticatedClient(founder)
                .PostAsJsonAsync("api/expert-collaboration-requests", new
                {
                    startup_id = startupId,
                    collaboration_type = (int)CollaborationType.Advisor,
                });

            invite.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            ProblemDto? problem = await invite.Content.ReadFromJsonAsync<ProblemDto>();
            problem.ShouldNotBeNull();
            problem.Title.ShouldBe("ExpertCollaborationRequests.ExpertProfileIdRequired");
        }

        [Fact]
        public async Task AnExpertCannotFileARequestUnderSomeoneElsesName()
        {
            User expert = await SeedUserAsync();
            User otherExpert = await SeedUserAsync();
            await SeedExpertProfileAsync(expert.Id);
            await SeedExpertProfileAsync(otherExpert.Id);
            Guid startupId = await SeedStartupAsync((await SeedUserAsync()).Id, StartupRole.Founder);

            HttpResponseMessage apply = await CreateAuthenticatedClient(expert)
                .PostAsJsonAsync("api/expert-collaboration-requests", new
                {
                    startup_id = startupId,
                    expert_profile_id = otherExpert.Id,
                    collaboration_type = (int)CollaborationType.Advisor,
                });

            apply.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            ProblemDto? problem = await apply.Content.ReadFromJsonAsync<ProblemDto>();
            problem.ShouldNotBeNull();
            problem.Title.ShouldBe("ExpertCollaborationRequests.Unauthorized");
        }

        [Fact]
        public async Task BannedStartupsAreOutOfScope()
        {
            User expert = await SeedUserAsync();
            await SeedExpertProfileAsync(expert.Id);
            Guid startupId = await SeedStartupAsync((await SeedUserAsync()).Id, StartupRole.Founder);

            await ExecuteDbAsync(async db =>
            {
                Startup startup = await db.Startups.SingleAsync(s => s.Id == startupId);
                startup.Ban("spam", expiresAt: null, byUserId: Guid.NewGuid(), DateTime.UtcNow);
                await db.SaveChangesAsync();
            });

            HttpResponseMessage apply = await CreateAuthenticatedClient(expert)
                .PostAsJsonAsync("api/expert-collaboration-requests", new
                {
                    startup_id = startupId,
                    collaboration_type = (int)CollaborationType.Advisor,
                });

            apply.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            ProblemDto? problem = await apply.Content.ReadFromJsonAsync<ProblemDto>();
            problem.ShouldNotBeNull();
            problem.Title.ShouldBe("ExpertCollaborationRequests.StartupUnavailable");
        }

        [Fact]
        public async Task ListsHonourTheStatusFilterAndPageSize()
        {
            User founder = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync((founder.Id, StartupRole.Founder));

            User first = await SeedUserAsync();
            User second = await SeedUserAsync();
            await SeedExpertProfileAsync(first.Id);
            await SeedExpertProfileAsync(second.Id);
            Guid pendingId = await ApplyAsync(first, startupId);
            Guid rejectedId = await ApplyAsync(second, startupId);

            HttpResponseMessage reject = await CreateAuthenticatedClient(founder)
                .PostAsJsonAsync($"api/expert-collaboration-requests/{rejectedId}/reject", new { });
            reject.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            string listUrl = $"api/startups/{startupId}/expert-collaboration-requests";

            List<RequestDto> rejectedOnly = await ListAsync(
                founder, $"{listUrl}?status={(int)ExpertCollaborationRequestStatus.Rejected}");
            rejectedOnly.Select(r => r.Id).ShouldBe([rejectedId]);

            // Pending leads regardless of age, so a one-row page returns the actionable one.
            List<RequestDto> firstPage = await ListAsync(founder, $"{listUrl}?pageNumber=1&pageSize=1");
            firstPage.Select(r => r.Id).ShouldBe([pendingId]);

            List<RequestDto> secondPage = await ListAsync(founder, $"{listUrl}?pageNumber=2&pageSize=1");
            secondPage.Select(r => r.Id).ShouldBe([rejectedId]);
        }

        [Fact]
        public async Task AStartupsRequestsAreNotReadableByOutsiders()
        {
            User founder = await SeedUserAsync();
            User outsider = await SeedUserAsync();
            Guid startupId = await SeedStartupAsync((founder.Id, StartupRole.Founder));

            HttpResponseMessage read = await CreateAuthenticatedClient(outsider)
                .GetAsync($"api/startups/{startupId}/expert-collaboration-requests");

            read.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        private async Task<Guid> ApplyAsync(User expert, Guid startupId)
        {
            HttpResponseMessage apply = await CreateAuthenticatedClient(expert)
                .PostAsJsonAsync("api/expert-collaboration-requests", new
                {
                    startup_id = startupId,
                    collaboration_type = (int)CollaborationType.Consultant,
                });
            apply.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await apply.Content.ReadFromJsonAsync<Guid>();
        }

        private async Task<List<RequestDto>> ListAsync(User user, string url)
        {
            List<RequestDto>? rows = await CreateAuthenticatedClient(user).GetFromJsonAsync<List<RequestDto>>(url);
            rows.ShouldNotBeNull();
            return rows;
        }

        private Task SeedExpertProfileAsync(Guid userId) => ExecuteDbAsync(async db =>
        {
            db.ExpertProfiles.Add(ExpertProfile.Create(userId, DateTime.UtcNow));
            await db.SaveChangesAsync();
        });

        private Task<Guid> SeedStartupAsync(Guid founderId, StartupRole role) => SeedStartupAsync((founderId, role));

        private async Task<Guid> SeedStartupAsync(params (Guid ProfileId, StartupRole Role)[] members)
        {
            Guid startupId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                DateTime now = DateTime.UtcNow;
                db.Startups.Add(new Startup
                {
                    Id = startupId,
                    Name = $"Collab Co {startupId:N}"[..22],
                    PublicEmail = "team@example.com",
                    Stage = StartupStage.Seed,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                foreach ((Guid profileId, StartupRole role) in members)
                {
                    db.StartupMembers.Add(StartupMember.Create(profileId, startupId, role, isPublic: true, now));
                }

                await db.SaveChangesAsync();
            });
            return startupId;
        }
    }
}
