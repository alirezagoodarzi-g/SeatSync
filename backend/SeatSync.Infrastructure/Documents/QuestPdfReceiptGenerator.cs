using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SeatSync.Core.DTOs;
using SeatSync.Core.Interfaces;

namespace SeatSync.Infrastructure.Documents;

public class QuestPdfReceiptGenerator : IReceiptGenerator
{
    static QuestPdfReceiptGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var fontsDir = Path.Combine(AppContext.BaseDirectory, "Fonts");
        FontManager.RegisterFont(File.OpenRead(Path.Combine(fontsDir, "Vazirmatn-Regular.ttf")));
        FontManager.RegisterFont(File.OpenRead(Path.Combine(fontsDir, "Vazirmatn-Bold.ttf")));
    }

    public byte[] GenerateBookingReceipt(ReceiptData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Vazirmatn").FontSize(11).DirectionFromRightToLeft());
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text("رسید خرید بلیط").FontSize(18).Bold();
                    col.Item().AlignCenter().Text("سیت‌سینک").FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingVertical(12);

                    col.Item().Text(t =>
                    {
                        t.Span("رویداد: ").Bold();
                        t.Span(data.EventName);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("مکان: ").Bold();
                        t.Span(data.Venue);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("خریدار: ").Bold();
                        t.Span(data.CustomerName);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("تاریخ رویداد (UTC): ").Bold();
                        t.Span(data.EventDateTimeUtc.ToString("yyyy-MM-dd HH:mm"));
                    });

                    col.Item().PaddingVertical(14);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("شماره صندلی").Bold();
                            header.Cell().Text("قیمت").Bold();
                            header.Cell().ColumnSpan(2).PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                        });

                        foreach (var seat in data.Seats)
                        {
                            table.Cell().PaddingVertical(4).Text(seat.RowNumber);
                            table.Cell().PaddingVertical(4).Text($"{seat.Price:N0} تومان");
                        }
                    });

                    col.Item().PaddingVertical(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"تعداد بلیط‌ها: {data.Seats.Count}");
                        row.RelativeItem().AlignLeft().Text($"مجموع هزینه: {data.TotalPrice:N0} تومان").Bold();
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("این رسید به صورت الکترونیکی صادر شده است.").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}