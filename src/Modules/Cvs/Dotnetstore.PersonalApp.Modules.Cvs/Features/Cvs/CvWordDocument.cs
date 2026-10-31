using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Dotnetstore.PersonalApp.Shared.Localization;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Lays a CV out as a Word document (A4, one column). Headings are in the current UI language.</summary>
internal static class CvWordDocument
{
    private const string Accent = "3730A3";
    private const string Muted = "6B7280";
    private const long PhotoWidthEmu = 1_260_000; // 3.5 cm
    private const long PhotoMaxHeightEmu = 1_800_000; // 5 cm
    private const string TextColumnWidth = "7200";
    private const string PhotoColumnWidth = "2438";

    public static byte[] Build(CvDetailDto detail, ILocalizationService localization)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            AddStyles(main);

            var body = main.Document.Body!;
            AddHeader(body, main, detail, localization);
            AddBody(body, detail, localization);

            body.Append(new SectionProperties(
                new PageSize { Width = 11906U, Height = 16838U },
                new PageMargin { Top = 1134, Right = 1134U, Bottom = 1134, Left = 1134U, Header = 708U, Footer = 708U, Gutter = 0U }));

            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AddStyles(MainDocumentPart main)
    {
        var styles = main.AddNewPart<StyleDefinitionsPart>();
        styles.Styles = new Styles(new DocDefaults(
            new RunPropertiesDefault(new RunPropertiesBaseStyle(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Microsoft YaHei" },
                new FontSize { Val = "21" })),
            new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                new SpacingBetweenLines { After = "80", Line = "264", LineRule = LineSpacingRuleValues.Auto }))));
    }

    private static void AddHeader(Body body, MainDocumentPart main, CvDetailDto detail, ILocalizationService localization)
    {
        var name = detail.User?.FullName ?? localization.Get("CvDetail_UserMissing");

        var text = new TableCell(
            new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = TextColumnWidth }),
            Paragraph(name, bold: true, size: 44, after: 40));
        if (!string.IsNullOrEmpty(detail.Cv.TargetJob))
        {
            text.Append(Paragraph(detail.Cv.TargetJob, size: 26, color: Accent, after: 40));
        }

        text.Append(Paragraph(detail.Cv.Title, color: Muted, after: 40));
        if (detail.User is { } user)
        {
            text.Append(Paragraph(
                $"{localization.Get("CvDetail_DateOfBirth")}: {user.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}", color: Muted));
        }

        foreach (var contact in detail.SelectedContacts)
        {
            text.Append(Paragraph($"{contact.TypeName}: {contact.Value}", after: 20));
        }

        if (detail.Cv.LinkedIn is { } linkedIn)
        {
            text.Append(Paragraph($"LinkedIn: {linkedIn}", after: 20));
        }

        var photo = new TableCell(
            new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = PhotoColumnWidth }),
            detail.Photo is { } bytes ? PhotoParagraph(main, bytes) : new Paragraph());

        body.Append(new Table(
            new TableProperties(new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "9638" }),
            new TableGrid(new GridColumn { Width = TextColumnWidth }, new GridColumn { Width = PhotoColumnWidth }),
            new TableRow(text, photo)));
    }

    private static void AddBody(Body body, CvDetailDto detail, ILocalizationService localization)
    {
        var present = localization.Get("CvDetail_Present");

        var technologies = detail.WorkExperiences
            .SelectMany(w => w.Technologies)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        if (technologies.Count > 0)
        {
            body.Append(Heading(localization.Get("CvDetail_Technologies")));
            body.Append(Paragraph(string.Join(" · ", technologies)));
        }

        if (!string.IsNullOrWhiteSpace(detail.Cv.Summary))
        {
            body.Append(Heading(localization.Get("CvDetail_Summary")));
            body.Append(Paragraph(detail.Cv.Summary));
        }

        if (detail.WorkExperiences.Count > 0)
        {
            body.Append(Heading(localization.Get("CvSection_Work")));
            foreach (var job in detail.WorkExperiences)
            {
                body.Append(Paragraph($"{job.Role}, {job.Employer}", bold: true, after: 0, keepNext: true));
                body.Append(Paragraph(PeriodFormat.Format(job.StartDate, job.EndDate, present), italic: true, color: Muted, after: 40, keepNext: true));
                if (!string.IsNullOrWhiteSpace(job.Description))
                {
                    body.Append(Paragraph(job.Description, after: 40));
                }

                if (job.Technologies.Count > 0)
                {
                    body.Append(Paragraph($"{localization.Get("CvDetail_Technologies")}: {string.Join(", ", job.Technologies)}", color: Muted, after: 140));
                }
            }
        }

        if (detail.Educations.Count > 0)
        {
            body.Append(Heading(localization.Get("CvSection_Education")));
            foreach (var education in detail.Educations)
            {
                body.Append(Paragraph($"{education.Programme}, {education.School}", bold: true, after: 0, keepNext: true));
                body.Append(Paragraph(PeriodFormat.Format(education.StartDate, education.EndDate, present), italic: true, color: Muted, after: 40, keepNext: true));
                if (!string.IsNullOrWhiteSpace(education.Description))
                {
                    body.Append(Paragraph(education.Description, after: 140));
                }
            }
        }

        if (detail.Courses.Count > 0)
        {
            body.Append(Heading(localization.Get("CvSection_Courses")));
            foreach (var course in detail.Courses)
            {
                var parts = new[] { course.Name, course.Provider, course.CompletedYear?.ToString(CultureInfo.InvariantCulture) }
                    .Where(p => !string.IsNullOrEmpty(p));
                body.Append(Paragraph(string.Join(" — ", parts), after: 40));
            }
        }

        if (detail.Languages.Count > 0)
        {
            body.Append(Heading(localization.Get("CvSection_Languages")));
            foreach (var language in detail.Languages)
            {
                body.Append(Paragraph($"{language.Language} — {LanguageEditorViewModel.LevelText(localization, language.Level)}", after: 40));
            }
        }

        if (detail.DriverLicence.Count > 0)
        {
            body.Append(Heading(localization.Get("CvDetail_DriverLicence")));
            body.Append(Paragraph(string.Join(", ", detail.DriverLicence)));
        }
    }

    private static Paragraph Heading(string text) => new(
        new ParagraphProperties(
            new KeepNext(),
            new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 6U, Space = 1U, Color = "D1D5DB" }),
            new SpacingBetweenLines { Before = "280", After = "100" }),
        new Run(
            new RunProperties(new Bold(), new Color { Val = Accent }, new FontSize { Val = "26" }),
            new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

    /// <summary>A paragraph of text; line breaks in <paramref name="text"/> are kept. <paramref name="size"/> is in half-points, 0 for the default.</summary>
    private static Paragraph Paragraph(string text, bool bold = false, bool italic = false, int size = 0, string? color = null, int after = 80, bool keepNext = false)
    {
        var properties = new ParagraphProperties();
        if (keepNext)
        {
            properties.Append(new KeepNext());
        }

        properties.Append(new SpacingBetweenLines { After = after.ToString(CultureInfo.InvariantCulture) });

        var runProperties = new RunProperties();
        if (bold)
        {
            runProperties.Append(new Bold());
        }

        if (italic)
        {
            runProperties.Append(new Italic());
        }

        if (color is not null)
        {
            runProperties.Append(new Color { Val = color });
        }

        if (size > 0)
        {
            runProperties.Append(new FontSize { Val = size.ToString(CultureInfo.InvariantCulture) });
        }

        var run = new Run(runProperties);
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                run.Append(new Break());
            }

            run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }

        return new Paragraph(properties, run);
    }

    private static Paragraph PhotoParagraph(MainDocumentPart main, byte[] photo)
    {
        var imagePart = main.AddImagePart(ImageSize.IsPng(photo) ? ImagePartType.Png : ImagePartType.Jpeg);
        using (var imageStream = new MemoryStream(photo))
        {
            imagePart.FeedData(imageStream);
        }

        var width = PhotoWidthEmu;
        var height = PhotoWidthEmu;
        if (ImageSize.TryRead(photo) is { Width: > 0, Height: > 0 } size)
        {
            height = width * size.Height / size.Width;
            if (height > PhotoMaxHeightEmu)
            {
                width = width * PhotoMaxHeightEmu / height;
                height = PhotoMaxHeightEmu;
            }
        }

        var relationshipId = main.GetIdOfPart(imagePart);
        var inline = new DW.Inline(
            new DW.Extent { Cx = width, Cy = height },
            new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
            new DW.DocProperties { Id = 1U, Name = "Photo" },
            new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
            new A.Graphic(new A.GraphicData(
                new PIC.Picture(
                    new PIC.NonVisualPictureProperties(
                        new PIC.NonVisualDrawingProperties { Id = 0U, Name = "photo" },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(new A.Blip { Embed = relationshipId }, new A.Stretch(new A.FillRectangle())),
                    new PIC.ShapeProperties(
                        new A.Transform2D(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = width, Cy = height }),
                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U,
        };

        return new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
            new Run(new Drawing(inline)));
    }
}
