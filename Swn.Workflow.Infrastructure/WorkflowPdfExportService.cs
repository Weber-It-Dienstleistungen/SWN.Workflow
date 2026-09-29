using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Swn.Workflow.Application;

using MigraDocDocument =
    MigraDoc.DocumentObjectModel.Document;

using MigraDocSection =
    MigraDoc.DocumentObjectModel.Section;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowPdfExportService
    : IWorkflowPdfExportService
{
    private static readonly object FontConfigurationLock =
        new();

    private static bool _fontConfigurationInitialized;

    private static readonly Color AccentColor =
        Color.FromRgb(
            34,
            92,
            145);

    private static readonly Color AccentLightColor =
        Color.FromRgb(
            232,
            240,
            247);

    private static readonly Color BorderColor =
        Color.FromRgb(
            205,
            211,
            218);

    private static readonly Color MutedColor =
        Color.FromRgb(
            100,
            108,
            116);

    private static readonly Color SuccessColor =
        Color.FromRgb(
            25,
            135,
            84);

    private static readonly Color SuccessLightColor =
        Color.FromRgb(
            224,
            242,
            233);

    private static readonly Color NotRequiredColor =
        Color.FromRgb(
            108,
            117,
            125);

    private readonly IWorkflowDetailService
        _workflowDetailService;

    public WorkflowPdfExportService(
        IWorkflowDetailService workflowDetailService)
    {
        ArgumentNullException.ThrowIfNull(
            workflowDetailService);

        _workflowDetailService =
            workflowDetailService;
    }

    public async Task<WorkflowPdfExportResult?>
        CreateForInitiatorAsync(
            Guid workflowInstanceId,
            string userId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            userId);

        var normalizedUserId =
            userId.Trim();

        if (string.IsNullOrWhiteSpace(
            normalizedUserId))
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        var workflow =
            await _workflowDetailService
                .GetForInitiatorAsync(
                    workflowInstanceId,
                    normalizedUserId,
                    cancellationToken);

        if (workflow is null)
        {
            return null;
        }

        if (workflow.Status != 2)
        {
            throw new InvalidOperationException(
                "Ein PDF-Export ist erst möglich, " +
                "wenn der Workflow vollständig abgeschlossen ist.");
        }

        EnsureFontConfiguration();

        var document =
            CreateDocument(
                workflow);

        var renderer =
            new PdfDocumentRenderer
            {
                Document =
                    document
            };

        renderer.RenderDocument();

        using var stream =
            new MemoryStream();

        renderer.PdfDocument.Save(
            stream,
            false);

        return new WorkflowPdfExportResult(
            stream.ToArray(),
            CreateFileName(
                workflow));
    }

    private static MigraDocDocument CreateDocument(
        WorkflowDetailItem workflow)
    {
        var document =
            new MigraDocDocument();

        document.Info.Title =
            $"SWN Workflow - {workflow.Subject}";

        document.Info.Subject =
            workflow.WorkflowName;

        ConfigureStyles(
            document);

        var section =
            document.AddSection();

        section.PageSetup.PageFormat =
            PageFormat.A4;

        section.PageSetup.TopMargin =
            Unit.FromCentimeter(1.5);

        section.PageSetup.BottomMargin =
            Unit.FromCentimeter(1.5);

        section.PageSetup.LeftMargin =
            Unit.FromCentimeter(1.6);

        section.PageSetup.RightMargin =
            Unit.FromCentimeter(1.6);

        AddDocumentHeader(
            section,
            workflow);

        AddSummary(
            section,
            workflow);

        AddProperties(
            section,
            workflow);

        AddTaskOverview(
            section,
            workflow);

        AddFooter(
            section);

        return document;
    }

    private static void ConfigureStyles(
        MigraDocDocument document)
    {
        var normalStyle =
            document.Styles[
                StyleNames.Normal];

        normalStyle.Font.Name =
            "Arial";

        normalStyle.Font.Size =
            Unit.FromPoint(9);

        normalStyle.ParagraphFormat.SpaceAfter =
            Unit.FromPoint(3);

        var heading1 =
            document.Styles[
                StyleNames.Heading1];

        heading1.Font.Name =
            "Arial";

        heading1.Font.Size =
            Unit.FromPoint(19);

        heading1.Font.Bold =
            true;

        heading1.Font.Color =
            AccentColor;

        heading1.ParagraphFormat.SpaceBefore =
            Unit.FromPoint(4);

        heading1.ParagraphFormat.SpaceAfter =
            Unit.FromPoint(4);

        heading1.ParagraphFormat.KeepWithNext =
            true;

        var heading2 =
            document.Styles[
                StyleNames.Heading2];

        heading2.Font.Name =
            "Arial";

        heading2.Font.Size =
            Unit.FromPoint(12);

        heading2.Font.Bold =
            true;

        heading2.Font.Color =
            AccentColor;

        heading2.ParagraphFormat.SpaceBefore =
            Unit.FromPoint(12);

        heading2.ParagraphFormat.SpaceAfter =
            Unit.FromPoint(6);

        heading2.ParagraphFormat.KeepWithNext =
            true;
    }

    private static void AddDocumentHeader(
        MigraDocSection section,
        WorkflowDetailItem workflow)
    {
        var headerTable =
            section.AddTable();

        headerTable.AddColumn(
            Unit.FromCentimeter(12.8));

        headerTable.AddColumn(
            Unit.FromCentimeter(4));

        var row =
            headerTable.AddRow();

        var leftCell =
            row.Cells[0];

        var organisation =
            leftCell.AddParagraph();

        organisation.Format.SpaceAfter =
            Unit.FromPoint(5);

        var organisationText =
            organisation.AddFormattedText(
                "SWN STADTWERKE NEUSTADT");

        organisationText.Bold =
            true;

        organisationText.Font.Size =
            Unit.FromPoint(9);

        organisationText.Font.Color =
            MutedColor;

        var subject =
            leftCell.AddParagraph();

        subject.Style =
            StyleNames.Heading1;

        subject.AddText(
            workflow.Subject);

        var workflowName =
            leftCell.AddParagraph();

        workflowName.Format.SpaceAfter =
            Unit.FromPoint(3);

        var workflowText =
            workflowName.AddFormattedText(
                workflow.WorkflowName);

        workflowText.Bold =
            true;

        workflowText.Font.Size =
            Unit.FromPoint(10.5);

        var statusCell =
            row.Cells[1];

        statusCell.Shading.Color =
            SuccessLightColor;

        statusCell.VerticalAlignment =
            VerticalAlignment.Center;

        statusCell.Borders.Color =
            SuccessColor;

        statusCell.Borders.Width =
            Unit.FromPoint(0.8);

        var status =
            statusCell.AddParagraph();

        status.Format.Alignment =
            ParagraphAlignment.Center;

        status.Format.SpaceBefore =
            Unit.FromPoint(8);

        status.Format.SpaceAfter =
            Unit.FromPoint(8);

        var statusText =
            status.AddFormattedText(
                "ABGESCHLOSSEN");

        statusText.Bold =
            true;

        statusText.Font.Size =
            Unit.FromPoint(10);

        statusText.Font.Color =
            SuccessColor;

        var separator =
            section.AddParagraph();

        separator.Format.Borders.Bottom.Color =
            AccentColor;

        separator.Format.Borders.Bottom.Width =
            Unit.FromPoint(1.4);

        separator.Format.SpaceAfter =
            Unit.FromPoint(10);
    }

    private static void AddSummary(
        MigraDocSection section,
        WorkflowDetailItem workflow)
    {
        section.AddParagraph(
            "Vorgangsübersicht",
            StyleNames.Heading2);

        var table =
            section.AddTable();

        table.Borders.Color =
            BorderColor;

        table.Borders.Width =
            Unit.FromPoint(0.5);

        table.AddColumn(
            Unit.FromCentimeter(2.8));

        table.AddColumn(
            Unit.FromCentimeter(5.6));

        table.AddColumn(
            Unit.FromCentimeter(2.8));

        table.AddColumn(
            Unit.FromCentimeter(5.6));

        AddSummaryRow(
            table,
            "Stichtag",
            workflow.ReferenceDate
                .ToString(
                    "dd.MM.yyyy"),
            "Aufgaben",
            $"{workflow.CompletedTasks} von " +
            $"{workflow.TotalTasks}");

        AddSummaryRow(
            table,
            "Angelegt",
            workflow.CreatedAt
                .ToLocalTime()
                .ToString(
                    "dd.MM.yyyy HH:mm"),
            "Abgeschlossen",
            workflow.CompletedAt?
                .ToLocalTime()
                .ToString(
                    "dd.MM.yyyy HH:mm")
            ?? "-");
    }

    private static void AddSummaryRow(
        Table table,
        string firstLabel,
        string firstValue,
        string secondLabel,
        string secondValue)
    {
        var row =
            table.AddRow();

        FormatSummaryLabelCell(
            row.Cells[0],
            firstLabel);

        FormatSummaryValueCell(
            row.Cells[1],
            firstValue);

        FormatSummaryLabelCell(
            row.Cells[2],
            secondLabel);

        FormatSummaryValueCell(
            row.Cells[3],
            secondValue);
    }

    private static void FormatSummaryLabelCell(
        Cell cell,
        string label)
    {
        cell.Shading.Color =
            AccentLightColor;

        var paragraph =
            cell.AddParagraph();

        paragraph.Format.SpaceBefore =
            Unit.FromPoint(3);

        paragraph.Format.SpaceAfter =
            Unit.FromPoint(3);

        var text =
            paragraph.AddFormattedText(
                label);

        text.Bold =
            true;

        text.Font.Color =
            AccentColor;
    }

    private static void FormatSummaryValueCell(
        Cell cell,
        string value)
    {
        var paragraph =
            cell.AddParagraph();

        paragraph.Format.SpaceBefore =
            Unit.FromPoint(3);

        paragraph.Format.SpaceAfter =
            Unit.FromPoint(3);

        paragraph.AddText(
            value);
    }

    private static void AddProperties(
        MigraDocSection section,
        WorkflowDetailItem workflow)
    {
        if (workflow.Properties.Count == 0)
        {
            return;
        }

        section.AddParagraph(
            "Vorgangsdaten",
            StyleNames.Heading2);

        var table =
            section.AddTable();

        table.Borders.Color =
            BorderColor;

        table.Borders.Width =
            Unit.FromPoint(0.5);

        table.AddColumn(
            Unit.FromCentimeter(5));

        table.AddColumn(
            Unit.FromCentimeter(11.8));

        foreach (var property
            in workflow.Properties)
        {
            var row =
                table.AddRow();

            var labelCell =
                row.Cells[0];

            labelCell.Shading.Color =
                AccentLightColor;

            var labelParagraph =
                labelCell.AddParagraph();

            var labelText =
                labelParagraph.AddFormattedText(
                    property.Key);

            labelText.Bold =
                true;

            labelText.Font.Color =
                AccentColor;

            var valueParagraph =
                row.Cells[1]
                    .AddParagraph();

            valueParagraph.AddText(
                property.Value);
        }
    }

    private static void AddTaskOverview(
        MigraDocSection section,
        WorkflowDetailItem workflow)
    {
        section.AddParagraph(
            "Aufgabenübersicht",
            StyleNames.Heading2);

        var phases =
            workflow.Tasks
                .GroupBy(task =>
                    task.Phase)
                .OrderBy(group =>
                    group.Min(task =>
                        task.SortOrder))
                .ToArray();

        foreach (var phase
            in phases)
        {
            AddPhaseTable(
                section,
                phase.Key,
                phase
                    .OrderBy(task =>
                        task.SortOrder)
                    .ThenBy(task =>
                        task.Key)
                    .ToArray());
        }
    }

    private static void AddPhaseTable(
        MigraDocSection section,
        string phaseName,
        IReadOnlyCollection<WorkflowTaskDetailItem> tasks)
    {
        var spacing =
            section.AddParagraph();

        spacing.Format.SpaceAfter =
            Unit.FromPoint(3);

        var table =
            section.AddTable();

        table.Borders.Color =
            BorderColor;

        table.Borders.Width =
            Unit.FromPoint(0.5);

        table.AddColumn(
            Unit.FromCentimeter(9));

        table.AddColumn(
            Unit.FromCentimeter(3.2));

        table.AddColumn(
            Unit.FromCentimeter(4.6));

        var phaseRow =
            table.AddRow();

        phaseRow.HeadingFormat =
            true;

        phaseRow.KeepWith =
            1;

        phaseRow.Cells[0].MergeRight =
            2;

        phaseRow.Cells[0].Shading.Color =
            AccentColor;

        var phaseParagraph =
            phaseRow.Cells[0]
                .AddParagraph();

        phaseParagraph.Format.SpaceBefore =
            Unit.FromPoint(4);

        phaseParagraph.Format.SpaceAfter =
            Unit.FromPoint(4);

        var phaseText =
            phaseParagraph.AddFormattedText(
                phaseName);

        phaseText.Bold =
            true;

        phaseText.Font.Size =
            Unit.FromPoint(10.5);

        phaseText.Font.Color =
            Colors.White;

        var headerRow =
            table.AddRow();

        headerRow.HeadingFormat =
            true;

        headerRow.Shading.Color =
            AccentLightColor;

        FormatTableHeaderCell(
            headerRow.Cells[0],
            "Aufgabe");

        FormatTableHeaderCell(
            headerRow.Cells[1],
            "Status");

        FormatTableHeaderCell(
            headerRow.Cells[2],
            "Zuständig");

        foreach (var task
            in tasks)
        {
            AddTaskRow(
                table,
                task);
        }
    }

    private static void FormatTableHeaderCell(
        Cell cell,
        string text)
    {
        var paragraph =
            cell.AddParagraph();

        paragraph.Format.SpaceBefore =
            Unit.FromPoint(3);

        paragraph.Format.SpaceAfter =
            Unit.FromPoint(3);

        var formatted =
            paragraph.AddFormattedText(
                text);

        formatted.Bold =
            true;

        formatted.Font.Size =
            Unit.FromPoint(8.5);

        formatted.Font.Color =
            AccentColor;
    }

    private static void AddTaskRow(
        Table table,
        WorkflowTaskDetailItem task)
    {
        var row =
            table.AddRow();

        row.VerticalAlignment =
            VerticalAlignment.Top;

        var taskCell =
            row.Cells[0];

        var titleParagraph =
            taskCell.AddParagraph();

        titleParagraph.Format.SpaceBefore =
            Unit.FromPoint(3);

        titleParagraph.Format.SpaceAfter =
            Unit.FromPoint(2);

        var titleText =
            titleParagraph.AddFormattedText(
                task.Title);

        titleText.Bold =
            true;

        titleText.Font.Size =
            Unit.FromPoint(9);

        if (task.IsOptional)
        {
            var optionalText =
                titleParagraph.AddFormattedText(
                    "  optional");

            optionalText.Font.Size =
                Unit.FromPoint(7.5);

            optionalText.Font.Color =
                MutedColor;
        }

        if (task.CompletedAt is not null)
        {
            var completed =
                taskCell.AddParagraph();

            completed.Format.SpaceAfter =
                Unit.FromPoint(2);

            var completedText =
                completed.AddFormattedText(
                    "Erledigt: " +
                    task.CompletedAt.Value
                        .ToLocalTime()
                        .ToString(
                            "dd.MM.yyyy HH:mm"));

            completedText.Font.Size =
                Unit.FromPoint(7.5);

            completedText.Font.Color =
                MutedColor;
        }

        if (!string.IsNullOrWhiteSpace(
            task.Comment))
        {
            var comment =
                taskCell.AddParagraph();

            comment.Format.SpaceBefore =
                Unit.FromPoint(2);

            comment.Format.SpaceAfter =
                Unit.FromPoint(3);

            var commentLabel =
                comment.AddFormattedText(
                    "Kommentar: ");

            commentLabel.Bold =
                true;

            commentLabel.Font.Size =
                Unit.FromPoint(7.5);

            commentLabel.Font.Color =
                MutedColor;

            var commentText =
                comment.AddFormattedText(
                    task.Comment.Trim());

            commentText.Font.Size =
                Unit.FromPoint(7.5);

            commentText.Font.Color =
                MutedColor;
        }

        var statusParagraph =
            row.Cells[1]
                .AddParagraph();

        statusParagraph.Format.SpaceBefore =
            Unit.FromPoint(3);

        statusParagraph.Format.SpaceAfter =
            Unit.FromPoint(3);

        var statusText =
            statusParagraph.AddFormattedText(
                GetTaskStatusText(
                    task.Status));

        statusText.Bold =
            true;

        statusText.Font.Size =
            Unit.FromPoint(8.5);

        statusText.Font.Color =
            GetTaskStatusColor(
                task.Status);

        var roleParagraph =
            row.Cells[2]
                .AddParagraph();

        roleParagraph.Format.SpaceBefore =
            Unit.FromPoint(3);

        roleParagraph.Format.SpaceAfter =
            Unit.FromPoint(3);

        var roleText =
            roleParagraph.AddFormattedText(
                task.AssignedRoleKey);

        roleText.Font.Size =
            Unit.FromPoint(8.5);
    }

    private static Color GetTaskStatusColor(
        int status)
    {
        return status switch
        {
            2 =>
                SuccessColor,

            4 =>
                NotRequiredColor,

            _ =>
                AccentColor
        };
    }

    private static void AddFooter(
        MigraDocSection section)
    {
        var footer =
            section.Footers.Primary
                .AddParagraph();

        footer.Format.Alignment =
            ParagraphAlignment.Center;

        footer.Format.Font.Size =
            Unit.FromPoint(7.5);

        footer.Format.Font.Color =
            MutedColor;

        footer.AddText(
            "SWN Workflow  |  Seite ");

        footer.AddPageField();

        footer.AddText(
            " von ");

        footer.AddNumPagesField();
    }

    private static string GetTaskStatusText(
        int status)
    {
        return status switch
        {
            0 => "Offen",
            1 => "In Bearbeitung",
            2 => "Erledigt",
            3 => "Blockiert",
            4 => "Nicht erforderlich",
            _ => "Unbekannt"
        };
    }

    private static string CreateFileName(
        WorkflowDetailItem workflow)
    {
        var rawName =
            $"SWN-Workflow_{workflow.WorkflowName}_" +
            workflow.Subject;

        var invalidCharacters =
            Path.GetInvalidFileNameChars();

        var sanitizedCharacters =
            rawName
                .Select(character =>
                    char.IsControl(character)
                    ||
                    invalidCharacters.Contains(
                        character)
                        ? '_'
                        : character)
                .ToArray();

        var sanitizedName =
            new string(
                    sanitizedCharacters)
                .Trim();

        while (sanitizedName.Contains(
            "__",
            StringComparison.Ordinal))
        {
            sanitizedName =
                sanitizedName.Replace(
                    "__",
                    "_",
                    StringComparison.Ordinal);
        }

        if (sanitizedName.Length > 120)
        {
            sanitizedName =
                sanitizedName[..120]
                    .Trim();
        }

        if (string.IsNullOrWhiteSpace(
            sanitizedName))
        {
            sanitizedName =
                $"SWN-Workflow_{workflow.Id:N}";
        }

        return
            $"{sanitizedName}.pdf";
    }

    private static void EnsureFontConfiguration()
    {
        if (_fontConfigurationInitialized)
        {
            return;
        }

        lock (FontConfigurationLock)
        {
            if (_fontConfigurationInitialized)
            {
                return;
            }

            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "Für den PDF-Export ist auf Nicht-Windows-Systemen " +
                    "noch ein eigener PDFsharp-FontResolver erforderlich.");
            }

            GlobalFontSettings.UseWindowsFontsUnderWindows =
                true;

            _fontConfigurationInitialized =
                true;
        }
    }
}