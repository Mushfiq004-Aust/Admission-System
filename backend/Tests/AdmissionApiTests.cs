using Xunit;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using api.Database;
using api.Models;

namespace SchoolAdmission.Tests;

[Collection("api")]
public class AdmissionApiTests
{
    private readonly AppFixture _app;

    public AdmissionApiTests(AppFixture app) => _app = app;

    // ---------------------------------------------------------------- 1
    [Fact]
    public async Task CreateClass_ReturnsCreated_WithZeroedSeatStatistics()
    {
        var created = await _app.CreateClassAsync(120, "Class 6 - Morning Shift");

        Assert.Equal("Class 6 - Morning Shift", created.Name);
        Assert.Equal(120, created.TotalSeats);

        var fetched = await _app.GetClassAsync(created.Id);
        Assert.Equal(120, fetched.AvailableSeats);
        Assert.Equal(0, fetched.HeldSeats);
        Assert.Equal(0, fetched.ConfirmedSeats);

        var list = await _app.Client.GetFromJsonAsync<List<ClassDto>>("/classes");
        Assert.Contains(list!, c => c.Id == created.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateClass_WithInvalidSeats_Returns400(int seats)
    {
        var res = await _app.PostClassAsync(new { name = "Bad", totalSeats = seats }, _app.AdminToken);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateClass_WithMissingName_Returns400()
    {
        var res = await _app.PostClassAsync(new { totalSeats = 10 }, _app.AdminToken);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---------------------------------------------------------------- 2
    [Fact]
    public async Task Apply_WithAvailableSeat_CreatesHeldHold()
    {
        var cls = await _app.CreateClassAsync(5);
        var student = AppFixture.NewStudentId();

        var hold = await _app.ApplyOkAsync(cls.Id, student);

        Assert.Equal("HELD", hold.Status);
        Assert.Equal(student, hold.StudentId);
        Assert.Equal(cls.Id, hold.ClassId);
        Assert.True(hold.ExpiresAt > DateTime.UtcNow);

        var stats = await _app.GetClassAsync(cls.Id);
        Assert.Equal(1, stats.HeldSeats);
        Assert.Equal(4, stats.AvailableSeats);
    }

    // ---------------------------------------------------------------- 3
    [Fact]
    public async Task Apply_WhenClassIsFull_Returns409()
    {
        var cls = await _app.CreateClassAsync(2);
        await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());
        await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());

        var res = await _app.ApplyAsync(cls.Id, AppFixture.NewStudentId());

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var stats = await _app.GetClassAsync(cls.Id);
        Assert.Equal(0, stats.AvailableSeats);
    }

    // ---------------------------------------------------------------- 4
    [Fact]
    public async Task Apply_SameStudentTwiceToSameClass_SecondIsRejected()
    {
        var cls = await _app.CreateClassAsync(10);
        var student = AppFixture.NewStudentId();

        await _app.ApplyOkAsync(cls.Id, student);
        var second = await _app.ApplyAsync(cls.Id, student);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, (await _app.GetClassAsync(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Apply_SameStudentToDifferentShifts_IsAllowed()
    {
        var morning = await _app.CreateClassAsync(5, "Class 6 - Morning Shift");
        var day = await _app.CreateClassAsync(5, "Class 6 - Day Shift");
        var student = AppFixture.NewStudentId();

        await _app.ApplyOkAsync(morning.Id, student);
        await _app.ApplyOkAsync(day.Id, student);

        Assert.Equal(1, (await _app.GetClassAsync(morning.Id)).HeldSeats);
        Assert.Equal(1, (await _app.GetClassAsync(day.Id)).HeldSeats);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("2015269123456789a")]
    [InlineData("201526912345678901")]
    public async Task Apply_WithMissingOrInvalidStudentId_Returns400(string? studentId)
    {
        var cls = await _app.CreateClassAsync(5);
        var res = await _app.SendAsync(HttpMethod.Post, $"/classes/{cls.Id}/holds", studentId);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Apply_ToUnknownOrMalformedClass_Returns404Or400()
    {
        var unknown = await _app.ApplyAsync(Guid.NewGuid(), AppFixture.NewStudentId());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var malformed = await _app.SendAsync(HttpMethod.Post, "/classes/not-a-guid/holds", AppFixture.NewStudentId());
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }

    // ---------------------------------------------------------------- 5
    [Fact]
    public async Task Confirm_ExpiredHold_IsRejected_EvenIfWorkerHasNotRunYet()
    {
        var cls = await _app.CreateClassAsync(3);
        var student = AppFixture.NewStudentId();
        // Row is still HELD in the DB but its time is already up.
        var holdId = await _app.SeedHoldAsync(cls.Id, student, HoldStatus.Held, DateTime.UtcNow.AddMinutes(-1));

        var res = await _app.ConfirmAsync(holdId, student);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var stats = await _app.GetClassAsync(cls.Id);
        Assert.Equal(0, stats.ConfirmedSeats);
        Assert.Equal(0, stats.HeldSeats);
        Assert.Equal(3, stats.AvailableSeats);

        var status = await _app.SendAsync(HttpMethod.Get, $"/holds/{holdId}", student);
        var body = await status.Content.ReadFromJsonAsync<HoldDto>();
        Assert.Equal("EXPIRED", body!.Status);
    }

    [Fact]
    public async Task ExpiredHold_FreesTheSeatForAnotherStudent()
    {
        var cls = await _app.CreateClassAsync(1);
        await _app.SeedHoldAsync(cls.Id, AppFixture.NewStudentId(), HoldStatus.Held, DateTime.UtcNow.AddSeconds(-30));

        Assert.Equal(1, (await _app.GetClassAsync(cls.Id)).AvailableSeats);

        var hold = await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());
        Assert.Equal("HELD", hold.Status);
    }

    // ---------------------------------------------------------------- 6
    [Fact]
    public async Task Confirm_RepeatedRequests_CountConfirmedSeatOnlyOnce()
    {
        var cls = await _app.CreateClassAsync(5);
        var student = AppFixture.NewStudentId();
        var hold = await _app.ApplyOkAsync(cls.Id, student);

        // Sequential repeats ...
        for (var i = 0; i < 3; i++)
        {
            var res = await _app.ConfirmAsync(hold.HoldId, student);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("CONFIRMED", (await res.Content.ReadFromJsonAsync<ConfirmDto>())!.Status);
        }

        // ... and a burst of simultaneous "double clicks".
        var burst = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _app.ConfirmAsync(hold.HoldId, student)));
        Assert.All(burst, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var stats = await _app.GetClassAsync(cls.Id);
        Assert.Equal(1, stats.ConfirmedSeats);
        Assert.Equal(0, stats.HeldSeats);
        Assert.Equal(4, stats.AvailableSeats);
    }

    [Fact]
    public async Task Confirm_ByDifferentStudent_Returns403_AndDoesNotConfirm()
    {
        var cls = await _app.CreateClassAsync(5);
        var owner = AppFixture.NewStudentId();
        var hold = await _app.ApplyOkAsync(cls.Id, owner);

        var res = await _app.ConfirmAsync(hold.HoldId, AppFixture.NewStudentId());

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(0, (await _app.GetClassAsync(cls.Id)).ConfirmedSeats);
    }

    [Fact]
    public async Task Confirm_UnknownHold_Returns404()
    {
        var res = await _app.ConfirmAsync(Guid.NewGuid(), AppFixture.NewStudentId());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---------------------------------------------------------------- 7
    [Fact]
    public async Task Cancel_SetsCancelled_AndReleasesCapacity()
    {
        var cls = await _app.CreateClassAsync(1);
        var student = AppFixture.NewStudentId();
        var hold = await _app.ApplyOkAsync(cls.Id, student);
        Assert.Equal(0, (await _app.GetClassAsync(cls.Id)).AvailableSeats);

        var cancel = await _app.SendAsync(HttpMethod.Delete, $"/holds/{hold.HoldId}", student);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var status = await _app.SendAsync(HttpMethod.Get, $"/holds/{hold.HoldId}", student);
        Assert.Equal("CANCELLED", (await status.Content.ReadFromJsonAsync<HoldDto>())!.Status);
        Assert.Equal(1, (await _app.GetClassAsync(cls.Id)).AvailableSeats);

        // The seat can be taken by someone else, and the same student can re-apply later.
        await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());
    }

    [Fact]
    public async Task Cancel_ConfirmedAdmission_Returns409()
    {
        var cls = await _app.CreateClassAsync(2);
        var student = AppFixture.NewStudentId();
        var hold = await _app.ApplyOkAsync(cls.Id, student);
        await _app.ConfirmAsync(hold.HoldId, student);

        var cancel = await _app.SendAsync(HttpMethod.Delete, $"/holds/{hold.HoldId}", student);

        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Equal(1, (await _app.GetClassAsync(cls.Id)).ConfirmedSeats);
    }

    [Fact]
    public async Task Cancel_ByDifferentStudent_Returns403()
    {
        var cls = await _app.CreateClassAsync(2);
        var hold = await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());

        var cancel = await _app.SendAsync(HttpMethod.Delete, $"/holds/{hold.HoldId}", AppFixture.NewStudentId());

        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal(1, (await _app.GetClassAsync(cls.Id)).HeldSeats);
    }

    // ---------------------------------------------------------------- GET /holds/{id}
    [Fact]
    public async Task GetHold_ReturnsRequiredFields_ForTheOwner()
    {
        var cls = await _app.CreateClassAsync(3);
        var student = AppFixture.NewStudentId();
        var hold = await _app.ApplyOkAsync(cls.Id, student);

        var res = await _app.SendAsync(HttpMethod.Get, $"/holds/{hold.HoldId}", student);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = (await res.Content.ReadFromJsonAsync<HoldDto>())!;
        Assert.Equal(hold.HoldId, body.HoldId);
        Assert.Equal(cls.Id, body.ClassId);
        Assert.Equal("HELD", body.Status);
        // PostgreSQL keeps microseconds, so compare with a tiny tolerance.
        Assert.True(Math.Abs((hold.ExpiresAt - body.ExpiresAt).TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task GetHold_WithoutHeader_Returns400_AndByAnotherStudent_Returns403()
    {
        var cls = await _app.CreateClassAsync(3);
        var hold = await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());

        var noHeader = await _app.SendAsync(HttpMethod.Get, $"/holds/{hold.HoldId}", null);
        Assert.Equal(HttpStatusCode.BadRequest, noHeader.StatusCode);

        var stranger = await _app.SendAsync(HttpMethod.Get, $"/holds/{hold.HoldId}", AppFixture.NewStudentId());
        Assert.Equal(HttpStatusCode.Forbidden, stranger.StatusCode);
    }

    [Fact]
    public async Task HoldDuration_ComesFromConfiguration()
    {
        // The fixture sets HOLD_DURATION_SECONDS=600.
        var cls = await _app.CreateClassAsync(3);
        var before = DateTime.UtcNow;
        var hold = await _app.ApplyOkAsync(cls.Id, AppFixture.NewStudentId());

        var seconds = (hold.ExpiresAt - before).TotalSeconds;
        Assert.InRange(seconds, 595, 606);
    }

    // ---------------------------------------------------------------- me/holds
    [Fact]
    public async Task MyHolds_ReturnsOnlyTheCallersApplications_WithClassName()
    {
        var a = await _app.CreateClassAsync(5, "Class 7 - Morning Shift");
        var b = await _app.CreateClassAsync(5, "Class 7 - Day Shift");
        var me = AppFixture.NewStudentId();
        await _app.ApplyOkAsync(a.Id, me);
        await _app.ApplyOkAsync(b.Id, me);
        await _app.ApplyOkAsync(a.Id, AppFixture.NewStudentId()); // someone else

        var res = await _app.SendAsync(HttpMethod.Get, "/me/holds", me);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();

        Assert.Equal(2, json!.Count);
        Assert.All(json, h =>
        {
            Assert.Equal(me, h.GetProperty("studentId").GetString());
            Assert.False(string.IsNullOrEmpty(h.GetProperty("className").GetString()));
        });
    }

    [Fact]
    public async Task MyHolds_WithoutHeader_Returns400()
    {
        var res = await _app.SendAsync(HttpMethod.Get, "/me/holds", null);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---------------------------------------------------------------- 8  CONCURRENCY
    [Fact]
    public async Task Concurrency_100SimultaneousRequests_For10Seats_ExactlyTenSucceed()
    {
        const int seats = 10, requests = 100;
        var cls = await _app.CreateClassAsync(seats);

        var statuses = await FireSimultaneouslyAsync(requests, _ => _app.ApplyAsync(cls.Id, AppFixture.NewStudentId()));

        Assert.Equal(seats, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(requests - seats, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.DoesNotContain(statuses, s => s != HttpStatusCode.Created && s != HttpStatusCode.Conflict);

        var stats = await _app.GetClassAsync(cls.Id);
        Assert.Equal(seats, stats.HeldSeats);
        Assert.Equal(0, stats.AvailableSeats);
        Assert.True(stats.HeldSeats + stats.ConfirmedSeats <= stats.TotalSeats);

        Assert.Equal(seats, await CountRowsAsync(cls.Id, HoldStatus.Held));
    }

    [Fact]
    public async Task Concurrency_LastSeat_With50Contenders_ExactlyOneWins()
    {
        var cls = await _app.CreateClassAsync(1);

        var statuses = await FireSimultaneouslyAsync(50, _ => _app.ApplyAsync(cls.Id, AppFixture.NewStudentId()));

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(49, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal(1, await CountRowsAsync(cls.Id, HoldStatus.Held));
    }

    [Fact]
    public async Task Concurrency_SameStudentApplyingManyTimes_CreatesOnlyOneHold()
    {
        var cls = await _app.CreateClassAsync(10);
        var student = AppFixture.NewStudentId();

        var statuses = await FireSimultaneouslyAsync(20, _ => _app.ApplyAsync(cls.Id, student));

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(19, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal(1, (await _app.GetClassAsync(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Concurrency_MixedApplyConfirmCancel_NeverExceedsCapacity()
    {
        const int seats = 5;
        var cls = await _app.CreateClassAsync(seats);

        // 40 students try to apply; every successful one then confirms or cancels at random.
        var rng = new Random(42);
        var results = await FireSimultaneouslyAsync(40, async _ =>
        {
            var student = AppFixture.NewStudentId();
            var apply = await _app.ApplyAsync(cls.Id, student);
            if (apply.StatusCode == HttpStatusCode.Created)
            {
                var hold = (await apply.Content.ReadFromJsonAsync<HoldDto>())!;
                bool confirm;
                lock (rng) { confirm = rng.Next(2) == 0; }
                if (confirm) await _app.ConfirmAsync(hold.HoldId, student);
                else await _app.SendAsync(HttpMethod.Delete, $"/holds/{hold.HoldId}", student);
            }
            return apply;
        });

        Assert.DoesNotContain(results, s => s != HttpStatusCode.Created && s != HttpStatusCode.Conflict);

        var stats = await _app.GetClassAsync(cls.Id);
        Assert.True(stats.HeldSeats + stats.ConfirmedSeats <= seats,
            $"Oversold! held={stats.HeldSeats} confirmed={stats.ConfirmedSeats} total={seats}");
        Assert.True(await CountRowsAsync(cls.Id, HoldStatus.Confirmed) <= seats);
    }

    // ---------------------------------------------------------------- helpers
    /// <summary>Starts all tasks behind a gate and releases them together to maximise contention.</summary>
    private static async Task<List<HttpStatusCode>> FireSimultaneouslyAsync(
        int count, Func<int, Task<HttpResponseMessage>> action)
    {
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(async () =>
        {
            await gate.Task;
            var res = await action(i);
            return res.StatusCode;
        })).ToList();

        gate.SetResult();
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<int> CountRowsAsync(Guid classId, string status)
    {
        using var scope = _app.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.AdmissionHolds.CountAsync(h => h.ClassId == classId && h.Status == status);
    }
}
