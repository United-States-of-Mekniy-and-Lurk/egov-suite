using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Egov.Platform.Documents;

public sealed class PdfSharpOfficialDocumentRenderer : IOfficialDocumentRenderer
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double Margin = 48;
    private const double ContentWidth = PageWidth - (Margin * 2);
    private static readonly object FontLock = new();
    private readonly OfficialDocumentPdfOptions _options;

    public PdfSharpOfficialDocumentRenderer()
        : this(new OfficialDocumentPdfOptions())
    {
    }

    public PdfSharpOfficialDocumentRenderer(OfficialDocumentPdfOptions options)
    {
        _options = options;
        lock (FontLock)
        {
            GlobalFontSettings.FontResolver ??= new OfficialDocumentFontResolver();
        }
    }

    public GeneratedDocument Render(OfficialDocument document)
    {
        using var pdf = new PdfDocument();
        pdf.Info.Title = document.Title;
        pdf.Info.Subject = document.Subtitle;
        pdf.Info.Author = _options.Issuer;
        pdf.Info.CreationDate = document.IssuedAt.UtcDateTime;

        var context = new RenderContext(pdf, document, _options);
        context.StartPage();
        context.DrawTitle();
        context.DrawFields();
        context.DrawAuditTrail();
        context.DrawSignature();
        context.Complete();

        using var stream = new MemoryStream();
        pdf.Save(stream, closeStream: false);
        return new GeneratedDocument(
            stream.ToArray(),
            "application/pdf",
            $"{_options.FileNamePrefix}-{document.DocumentNumber}.pdf");
    }

    private sealed class RenderContext
    {
        private static readonly XColor Ink = XColor.FromArgb(47, 47, 47);
        private static readonly XColor Muted = XColor.FromArgb(102, 102, 102);
        private static readonly XColor Border = XColor.FromArgb(200, 200, 200);
        private static readonly XColor Sunken = XColor.FromArgb(239, 239, 239);
        private static readonly XColor Brand = XColor.FromArgb(0, 148, 255);
        private static readonly XColor Header = XColor.FromArgb(26, 26, 26);
        private readonly PdfDocument _pdf;
        private readonly OfficialDocument _document;
        private readonly OfficialDocumentPdfOptions _options;
        private readonly XFont _body = new("Noto Sans", 9.5);
        private readonly XFont _bodyBold = new("Noto Sans", 9.5, XFontStyleEx.Bold);
        private readonly XFont _small = new("Noto Sans", 7.5);
        private readonly XFont _smallBold = new("Noto Sans", 7.5, XFontStyleEx.Bold);
        private readonly XFont _title = new("Noto Serif", 18, XFontStyleEx.Bold);
        private readonly XFont _signature = new("Noto Serif", 10, XFontStyleEx.Bold);
        private PdfPage _page = null!;
        private XGraphics _graphics = null!;
        private double _y;

        public RenderContext(
            PdfDocument pdf,
            OfficialDocument document,
            OfficialDocumentPdfOptions options)
        {
            _pdf = pdf;
            _document = document;
            _options = options;
        }

        public void StartPage()
        {
            _graphics?.Dispose();
            _page = _pdf.AddPage();
            _page.Width = XUnit.FromPoint(PageWidth);
            _page.Height = XUnit.FromPoint(PageHeight);
            _graphics = XGraphics.FromPdfPage(_page);
            DrawMasthead();
            _y = 106;
        }

        public void DrawTitle()
        {
            DrawText(_document.DocumentNumber, _small, Muted, Margin, _y, ContentWidth / 2);
            DrawText(
                $"{_document.IssuedAt:dd MMMM yyyy} · {_options.IssueLocation}",
                _small,
                Muted,
                Margin + ContentWidth / 2,
                _y,
                ContentWidth / 2,
                XStringFormats.TopRight);
            _y += 30;
            DrawText(_document.Title, _title, Ink, Margin, _y, ContentWidth, XStringFormats.TopCenter);
            _y += 27;
            DrawText(_document.Subtitle, _small, Muted, Margin, _y, ContentWidth, XStringFormats.TopCenter);
            _y += 24;
            DrawRule();
            _y += 18;
        }

        public void DrawFields()
        {
            foreach (var field in _document.Fields)
            {
                var valueLines = Wrap(field.Value, _body, ContentWidth - 132);
                EnsureSpace(Math.Max(20, valueLines.Count * 13) + 6);
                DrawText(field.Label, _bodyBold, Ink, Margin, _y, 120);
                DrawLines(valueLines, _body, Ink, Margin + 132, _y, ContentWidth - 132, 13);
                _y += Math.Max(20, valueLines.Count * 13) + 6;
            }
            _y += 10;
        }

        public void DrawAuditTrail()
        {
            EnsureSpace(70);
            DrawText(_options.AuditTrailTitle, _signature, Ink, Margin, _y, ContentWidth);
            _y += 22;
            DrawAuditHeader();
            foreach (var entry in _document.AuditTrail) DrawAuditRow(entry);
            _y += 12;
        }

        public void DrawSignature()
        {
            EnsureSpace(92);
            _y += 28;
            _graphics.DrawLine(new XPen(Ink, 0.8), Margin + 275, _y, Margin + ContentWidth, _y);
            _y += 8;
            DrawText(_document.Signature.Name, _signature, Ink, Margin + 275, _y, ContentWidth - 275);
            _y += 16;
            DrawText(_document.Signature.Role, _small, Muted, Margin + 275, _y, ContentWidth - 275);
        }

        public void Complete()
        {
            _graphics.Dispose();
            for (var index = 0; index < _pdf.PageCount; index++)
            {
                using var graphics = XGraphics.FromPdfPage(_pdf.Pages[index], XGraphicsPdfPageOptions.Append);
                graphics.DrawRectangle(new XSolidBrush(Sunken), 0, PageHeight - 42, PageWidth, 40);
                graphics.DrawRectangle(new XSolidBrush(Brand), 0, PageHeight - 2, PageWidth, 2);
                graphics.DrawString(
                    _document.DocumentNumber,
                    _small,
                    new XSolidBrush(Muted),
                    new XRect(Margin, PageHeight - 29, ContentWidth / 2, 12),
                    XStringFormats.TopLeft);
                graphics.DrawString(
                    $"Page {index + 1} of {_pdf.PageCount}",
                    _small,
                    new XSolidBrush(Muted),
                    new XRect(Margin + ContentWidth / 2, PageHeight - 29, ContentWidth / 2, 12),
                    XStringFormats.TopRight);
            }
        }

        private void DrawMasthead()
        {
            _graphics.DrawRectangle(new XSolidBrush(Header), 0, 0, PageWidth, 72);
            _graphics.DrawRectangle(new XSolidBrush(Brand), 0, 72, PageWidth, 3);
            DrawText(_options.Masthead, _title, XColors.White, Margin, 17, 80);
            DrawText(_options.MastheadSubtitle, _smallBold, XColors.White, Margin + 86, 18, 250);
            DrawText(_options.DocumentKind, _small, XColor.FromArgb(190, 190, 190), Margin + 86, 33, 250);
        }

        private void DrawAuditHeader()
        {
            EnsureSpace(24);
            _graphics.DrawRectangle(new XSolidBrush(Sunken), Margin, _y, ContentWidth, 22);
            DrawText("Time", _smallBold, Ink, Margin + 6, _y + 6, 92);
            DrawText("Action", _smallBold, Ink, Margin + 104, _y + 6, 130);
            DrawText("Actor and note", _smallBold, Ink, Margin + 240, _y + 6, ContentWidth - 246);
            _y += 22;
        }

        private void DrawAuditRow(OfficialDocumentAuditEntry entry)
        {
            var actorAndNote = string.IsNullOrWhiteSpace(entry.Note)
                ? entry.ActorName
                : $"{entry.ActorName} · {entry.Note}";
            var actionLines = Wrap(entry.Action, _small, 124);
            var detailLines = Wrap(actorAndNote, _small, ContentWidth - 252);
            var rowHeight = Math.Max(28, Math.Max(actionLines.Count, detailLines.Count) * 11 + 10);
            if (!HasSpace(rowHeight))
            {
                StartPage();
                DrawText($"{_options.AuditTrailTitle} (continued)", _signature, Ink, Margin, _y, ContentWidth);
                _y += 22;
                DrawAuditHeader();
            }

            _graphics.DrawLine(new XPen(Border, 0.5), Margin, _y + rowHeight, Margin + ContentWidth, _y + rowHeight);
            DrawText(entry.OccurredAt.ToString("yyyy-MM-dd HH:mm 'UTC'"), _small, Ink, Margin + 6, _y + 7, 92);
            DrawLines(actionLines, _small, Ink, Margin + 104, _y + 7, 130, 11);
            DrawLines(detailLines, _small, Ink, Margin + 240, _y + 7, ContentWidth - 246, 11);
            _y += rowHeight;
        }

        private void DrawRule()
            => _graphics.DrawLine(new XPen(Border, 0.75), Margin, _y, Margin + ContentWidth, _y);

        private void EnsureSpace(double height)
        {
            if (!HasSpace(height)) StartPage();
        }

        private bool HasSpace(double height) => _y + height < PageHeight - 62;

        private void DrawText(
            string text,
            XFont font,
            XColor color,
            double x,
            double y,
            double width,
            XStringFormat? format = null)
            => _graphics.DrawString(
                text,
                font,
                new XSolidBrush(color),
                new XRect(x, y, width, font.GetHeight() + 3),
                format ?? XStringFormats.TopLeft);

        private void DrawLines(
            IReadOnlyList<string> lines,
            XFont font,
            XColor color,
            double x,
            double y,
            double width,
            double lineHeight)
        {
            foreach (var line in lines)
            {
                DrawText(line, font, color, x, y, width);
                y += lineHeight;
            }
        }

        private IReadOnlyList<string> Wrap(string text, XFont font, double width)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
            {
                var line = string.Empty;
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
                    if (_graphics.MeasureString(candidate, font).Width <= width || string.IsNullOrEmpty(line))
                    {
                        line = candidate;
                        continue;
                    }

                    lines.Add(line);
                    line = word;
                }
                lines.Add(line);
            }
            return lines.Count == 0 ? [string.Empty] : lines;
        }
    }
}