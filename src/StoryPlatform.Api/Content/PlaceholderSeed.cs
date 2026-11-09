using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Content;

/// <summary>
/// Starting content for a fresh installation: the series Barista Life with Part 1–6 as produced in
/// the episode folders (titles, running times, stills, story). Release dates and links are not known
/// yet, so every episode is flagged <c>IsPlaceholder</c> and the website labels its date as
/// "Beispieltermin". The example dates are computed relative to the first start.
/// Replace them via PUT /v1/admin/series/barista-life once the real dates are fixed.
/// </summary>
public static class PlaceholderSeed
{
    public static ContentDocument Create(DateTimeOffset now)
    {
        var today = BerlinTime.DateOf(now);
        DateTimeOffset DaysFromToday(int days, int hour) => BerlinTime.At(today.AddDays(days), new TimeOnly(hour, 0));

        // Next Wednesday 18:00 that still lies ahead.
        var next = Enumerable.Range(0, 8)
            .Select(i => today.AddDays(i))
            .Where(d => d.DayOfWeek == DayOfWeek.Wednesday)
            .Select(d => BerlinTime.At(d, new TimeOnly(18, 0)))
            .First(at => at > now);

        return new ContentDocument
        {
            Channel = new ChannelDocument
            {
                Name = "XKStories",
                Claim = "Krimi und Mystery in Serie",
                Description = "Illustrierte Krimi- und Mystery-Geschichten in Fortsetzungen, mit festen Sendeterminen. Neue Folgen erscheinen auf Instagram und TikTok.",
                SocialLinks =
                [
                    new SocialLink { Platform = Platform.Instagram, Url = null },
                    new SocialLink { Platform = Platform.TikTok, Url = null },
                ],
            },
            Series =
            [
                new SeriesDocument
                {
                    Slug = "barista-life",
                    Title = "Barista Life",
                    Tagline = "„Bitte tu so, als würdest du mich kennen.“",
                    Description = "19 Uhr 58. Du willst dein Café gerade abschließen, da stürmt eine fremde Frau herein und bittet dich um Hilfe. Draußen vor der Scheibe steht ein Mann, und im Radio läuft eine Fahndungsmeldung. Wer lügt hier, und wer beobachtet wen? Eine illustrierte Krimiserie in Fortsetzungen, erzählt aus deiner Sicht. Die Szenen sind gezeichnet und zeigen nicht das Spiel.",
                    Genres = ["Krimi", "Mystery"],
                    State = SeriesState.Running,
                    CoverImage = "barista-life-cover.jpg",
                    HeroImage = "barista-life-hero.jpg",
                    SocialImage = "barista-life-social.jpg",
                    EpisodeTerm = "Part",
                    FeaturedRank = 1,
                    Seasons =
                    [
                        new SeasonDocument
                        {
                            Number = 1,
                            Episodes =
                            [
                                new EpisodeDocument
                                {
                                    Slug = "part-1-19-uhr-58",
                                    Number = 1,
                                    Title = "19 Uhr 58",
                                    Synopsis = "Kurz vor Ladenschluss stürmt eine Frau in dein Café: „Bitte tu so, als würdest du mich kennen.“ Draußen vor der Scheibe steht ein Mann und schaut direkt zu ihr. Dann meldet das Radio, dass die Polizei einen gefährlichen Mann sucht.",
                                    DurationSeconds = 86,
                                    ReleaseAt = DaysFromToday(-15, 18),
                                    Thumbnail = "barista-life-part-1.jpg",
                                    IsPlaceholder = true,
                                },
                                new EpisodeDocument
                                {
                                    Slug = "part-2-der-anruf",
                                    Number = 2,
                                    Title = "Der Anruf",
                                    Synopsis = "Der Mann vor dem Café ruft dich an. Die Frau sei nicht zufällig hier, sagt er, sie sei deinetwegen gekommen. Du prüfst heimlich die Aufnahmen deiner Überwachungskamera und entdeckst immer dieselbe Frau. Tag für Tag. Nacht für Nacht.",
                                    DurationSeconds = 77,
                                    ReleaseAt = DaysFromToday(-12, 18),
                                    Thumbnail = "barista-life-part-2.jpg",
                                    IsPlaceholder = true,
                                },
                                new EpisodeDocument
                                {
                                    Slug = "part-3-die-falle",
                                    Number = 3,
                                    Title = "Die Falle",
                                    Synopsis = "„FBI! Open up!“ Draußen zerbricht etwas, die Frau ist verschwunden, und ein Mann hämmert gegen die Glastür. Er behauptet, sie sei bewaffnet. Du wählst den Notruf. Doch du bist nicht allein im Gebäude.",
                                    DurationSeconds = 179,
                                    ReleaseAt = DaysFromToday(-9, 18),
                                    Thumbnail = "barista-life-part-3.jpg",
                                    IsPlaceholder = true,
                                },
                                new EpisodeDocument
                                {
                                    Slug = "part-4-das-erwachen",
                                    Number = 4,
                                    Title = "Das Erwachen",
                                    Synopsis = "Ein Schuss, ein weißer Blitz. Dann stehst du wieder hinter deinem Tresen. Ein Stammkunde will seinen schwarzen Kaffee wie immer, die Kaffeemaschine ist plötzlich uralt, und die Wanduhr zeigt 19:58. Es ist dein Café. Aber nicht deine Zeit.",
                                    DurationSeconds = 69,
                                    ReleaseAt = DaysFromToday(-6, 18),
                                    Thumbnail = "barista-life-part-4.jpg",
                                    IsPlaceholder = true,
                                },
                                new EpisodeDocument
                                {
                                    Slug = "part-5-der-mann-in-der-ecke",
                                    Number = 5,
                                    Title = "Der Mann in der Ecke",
                                    Synopsis = "Es ist 1997, und du willst wissen, warum. Der Stammkunde warnt dich leise: Ein Mann in der Ecke lässt dich seit einer Stunde nicht aus den Augen. Dann kommt er an den Tresen und schiebt dir ein Polaroid hin. Ein Foto von dir, heute Abend, durch dein eigenes Fenster aufgenommen.",
                                    DurationSeconds = 65,
                                    ReleaseAt = DaysFromToday(-3, 18),
                                    Thumbnail = "barista-life-part-5.jpg",
                                    IsPlaceholder = true,
                                },
                                new EpisodeDocument
                                {
                                    Slug = "part-6-nur-gestreift",
                                    Number = 6,
                                    Title = "Nur gestreift",
                                    Synopsis = "Blaulicht, ein Sanitäter, ein Verband am Arm. Der Schuss hat dich nur gestreift, du warst nur ein paar Minuten weg. Doch die fremde Frau ist verschwunden, und mit ihr alle Überwachungsaufnahmen der letzten Woche. Dann vibriert dein Handy.",
                                    DurationSeconds = 64,
                                    ReleaseAt = DaysFromToday(0, 18),
                                    Thumbnail = "barista-life-part-6.jpg",
                                    IsPlaceholder = true,
                                },
                                new EpisodeDocument
                                {
                                    Slug = "part-7",
                                    Number = 7,
                                    Title = "Titel folgt",
                                    Synopsis = "Die Fortsetzung von „Nur gestreift“.",
                                    ReleaseAt = next > DaysFromToday(0, 18) ? next : next.AddDays(7),
                                    Label = "Fortsetzung",
                                    IsPlaceholder = true,
                                },
                            ],
                        },
                    ],
                },
            ],
        };
    }

    /// <summary>Copies the bundled series images into the media library (existing files are kept).</summary>
    public static void CopyMedia(string mediaDirectory, ILogger log)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "SeedMedia");
        if (!Directory.Exists(source)) return;
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var target = Path.Combine(mediaDirectory, Path.GetFileName(file));
            if (!File.Exists(target)) File.Copy(file, target);
        }
        log.LogInformation("Serienbilder nach {Dir} kopiert", mediaDirectory);
    }
}
