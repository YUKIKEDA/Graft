using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Date &amp; time category E2E.
/// </summary>
[Collection(GalleryDateTimeCollection.Name)]
public sealed class DateTimeE2ETests
{
    private readonly GalleryDateTimeFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">DateTime fixture.</param>
    public DateTimeE2ETests(GalleryDateTimeFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Calendar and DatePicker reachability.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - CalendarPage_Main / DatePickerPage_Main
    ///
    /// Steps:
    /// - Navigate Calendar + DatePicker
    /// - ExpectVisible + Screenshot
    ///
    /// Expected:
    /// - Controls visible
    /// </remarks>
    [Fact]
    public async Task CalendarAndDatePicker_AreVisible()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_DateAndTime", "Nav_Calendar");
        await app.GetByAutomationId("CalendarPage_Main").ExpectVisibleAsync(true);
        await GalleryNav.NavigateAsync(app, "Nav_DateAndTime", "Nav_DatePicker");
        await app.GetByAutomationId("DatePickerPage_Main").ExpectVisibleAsync(true);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// CalendarDatePicker and TimePicker reachability.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - CalendarDatePickerPage_Main / TimePickerPage_Main
    ///
    /// Steps:
    /// - Navigate both leaves
    /// - ExpectVisible + Screenshot
    ///
    /// Expected:
    /// - Controls visible
    /// </remarks>
    [Fact]
    public async Task CalendarDatePickerAndTimePicker_AreVisible()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_DateAndTime", "Nav_CalendarDatePicker");
        await app.GetByAutomationId("CalendarDatePickerPage_Main").ExpectVisibleAsync(true);
        await GalleryNav.NavigateAsync(app, "Nav_DateAndTime", "Nav_TimePicker");
        await app.GetByAutomationId("TimePickerPage_Main").ExpectVisibleAsync(true);
        await GalleryNav.ShotAsync(app);
    }

    private async Task<GraftSession> ReadyAsync()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        return app;
    }
}
