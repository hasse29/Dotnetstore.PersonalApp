using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Modules.Cvs.Resources;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

/// <summary>Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.</summary>
internal sealed class CvsFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public CvsFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<CvsDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Localization.AddResources(CvsStrings.ResourceManager);
        Localization.SetCulture("en");

        Create = new CreateCv.Handler(DbFactory, new CreateCv.Validator(), Clock);
        Get = new GetCv.Handler(DbFactory);
        List = new ListCvs.Handler(DbFactory);
        Update = new UpdateCv.Handler(DbFactory, new UpdateCv.Validator(), Clock);
        Delete = new DeleteCv.Handler(DbFactory);
        DeleteForUser = new DeleteCvsForUser.Handler(DbFactory);

        GetDetail = new GetCvDetail.Handler(DbFactory, Users, [Contacts]);
        SaveWork = new SaveWorkExperience.Handler(DbFactory, new SaveWorkExperience.Validator());
        SaveEducation = new SaveEducation.Handler(DbFactory, new SaveEducation.Validator());
        SaveCourse = new SaveCourse.Handler(DbFactory, new SaveCourse.Validator(Clock));
        SaveLanguage = new SaveLanguageSkill.Handler(DbFactory, new SaveLanguageSkill.Validator());
        DeleteItem = new DeleteCvSectionItem.Handler(DbFactory);
        SetLicence = new SetDriverLicence.Handler(DbFactory);
        SetContacts = new SetCvContacts.Handler(DbFactory, [Contacts]);
        SetPhoto = new SetCvPhoto.Handler(DbFactory, Clock);
        Export = new ExportCvToWord.Handler(GetDetail, Localization);

        DetailServices = new CvDetailServices(
            GetDetail, SaveWork, SaveEducation, SaveCourse, SaveLanguage, DeleteItem, SetLicence, SetContacts, SetPhoto, Export, FileOpen, FileSave, Localization);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public Localizer Localization { get; } = new();

    public FakeUserLookup Users { get; } = new();

    public FakeContactSource Contacts { get; } = new();

    public FakeFileOpenService FileOpen { get; } = new();

    public IDbContextFactory<CvsDbContext> DbFactory { get; }

    public CreateCv.Handler Create { get; }

    public GetCv.Handler Get { get; }

    public ListCvs.Handler List { get; }

    public UpdateCv.Handler Update { get; }

    public DeleteCv.Handler Delete { get; }

    public DeleteCvsForUser.Handler DeleteForUser { get; }

    public GetCvDetail.Handler GetDetail { get; }

    public SaveWorkExperience.Handler SaveWork { get; }

    public SaveEducation.Handler SaveEducation { get; }

    public SaveCourse.Handler SaveCourse { get; }

    public SaveLanguageSkill.Handler SaveLanguage { get; }

    public DeleteCvSectionItem.Handler DeleteItem { get; }

    public SetDriverLicence.Handler SetLicence { get; }

    public SetCvContacts.Handler SetContacts { get; }

    public SetCvPhoto.Handler SetPhoto { get; }

    public ExportCvToWord.Handler Export { get; }

    public FakeFileSaveService FileSave { get; } = new();

    public CvDetailServices DetailServices { get; }

    public static CreateCv.Command ValidCv(Guid userId, string title = "Backend developer") =>
        new(userId, title, "Senior .NET developer", "Ten years of experience.");

    public void Dispose() => _connection.Dispose();

    internal sealed class FakeUserLookup : IUserLookup
    {
        public UserReference? User { get; set; }

        public Task<UserReference?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(User is { } user && user.Id == id ? user : null);
    }

    /// <summary>Stands in for the Addresses, PhoneNumbers and EmailAddresses modules: offers the entries it was given to their owner.</summary>
    internal sealed class FakeContactSource : IContactInfoSource
    {
        private readonly Dictionary<Guid, List<ContactEntry>> _byOwner = [];

        public ContactEntry Add(Guid ownerId, ContactKind kind, string typeName, string value)
        {
            var entry = new ContactEntry(Guid.NewGuid(), kind, typeName, value);
            if (!_byOwner.TryGetValue(ownerId, out var list))
            {
                _byOwner[ownerId] = list = [];
            }

            list.Add(entry);
            return entry;
        }

        public Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContactEntry>>(_byOwner.TryGetValue(objectId, out var list) ? [.. list] : []);
    }

    internal sealed class FakeFileSaveService : IFileSaveService
    {
        public string? SavedName { get; private set; }

        public byte[]? SavedContent { get; private set; }

        public Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken)
        {
            SavedName = suggestedFileName;
            SavedContent = content;
            return Task.FromResult<string?>(suggestedFileName);
        }
    }

    internal sealed class FakeFileOpenService : IFileOpenService
    {
        public PickedFile? Next { get; set; }

        public Task<PickedFile?> PickImageAsync(string title, long maxBytes, CancellationToken cancellationToken) =>
            Task.FromResult(Next);
    }

    private sealed class TestDbFactory(DbContextOptions<CvsDbContext> options) : IDbContextFactory<CvsDbContext>
    {
        public CvsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
