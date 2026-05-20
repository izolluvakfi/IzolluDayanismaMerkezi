using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Services;

public class MemberScholarshipDetail
{
    public string MemberName { get; set; } = string.Empty;
    public int PledgedCount { get; set; }
    public int RealizedCount { get; set; }
}

public class RoleBasedScholarshipPdf
{
    public string Role { get; set; } = string.Empty;
    public int PledgedCount { get; set; }
    public int RealizedCount { get; set; }
    public double Percentage { get; set; }
    public decimal CommittedAmount { get; set; }  // Total amount promised
    public decimal RealizedAmount { get; set; }   // Total amount actually paid
}

public class PdfService
{
    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates an SVG donut chart
    /// </summary>
    /// <param name="data">Dictionary with label and value</param>
    /// <param name="colors">List of hex colors for each segment</param>
    /// <param name="size">Size of the chart (width and height)</param>
    /// <param name="innerRadiusRatio">Ratio for inner radius (0.0 = pie, 0.7 = donut)</param>
    /// <returns>SVG string</returns>
    private string GenerateDonutChartSvg(Dictionary<string, int> data, List<string> colors, int size = 150, double innerRadiusRatio = 0.65)
    {
        var total = data.Values.Sum();
        if (total == 0) return string.Empty;

        var cx = size / 2.0;
        var cy = size / 2.0;
        var outerRadius = size / 2.0 - 5; // Margin for stroke
        var innerRadius = outerRadius * innerRadiusRatio;

        var svg = new System.Text.StringBuilder();
        svg.AppendLine($"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 {size} {size}\" xmlns=\"http://www.w3.org/2000/svg\">");
        
        double currentAngle = -90; // Start from top
        int colorIndex = 0;

        foreach (var item in data)
        {
            if (item.Value == 0) continue;

            var percentage = (double)item.Value / total;
            var sweepAngle = percentage * 360;
            var endAngle = currentAngle + sweepAngle;

            // Calculate arc points
            var startOuterX = cx + outerRadius * Math.Cos(currentAngle * Math.PI / 180);
            var startOuterY = cy + outerRadius * Math.Sin(currentAngle * Math.PI / 180);
            var endOuterX = cx + outerRadius * Math.Cos(endAngle * Math.PI / 180);
            var endOuterY = cy + outerRadius * Math.Sin(endAngle * Math.PI / 180);

            var startInnerX = cx + innerRadius * Math.Cos(endAngle * Math.PI / 180);
            var startInnerY = cy + innerRadius * Math.Sin(endAngle * Math.PI / 180);
            var endInnerX = cx + innerRadius * Math.Cos(currentAngle * Math.PI / 180);
            var endInnerY = cy + innerRadius * Math.Sin(currentAngle * Math.PI / 180);

            var largeArcFlag = sweepAngle > 180 ? 1 : 0;
            var color = colors[colorIndex % colors.Count];

            // Path for donut segment
            var path = $"M {startOuterX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {startOuterY.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                       $"A {outerRadius.ToString(System.Globalization.CultureInfo.InvariantCulture)} {outerRadius.ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 {largeArcFlag} 1 {endOuterX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {endOuterY.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                       $"L {startInnerX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {startInnerY.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                       $"A {innerRadius.ToString(System.Globalization.CultureInfo.InvariantCulture)} {innerRadius.ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 {largeArcFlag} 0 {endInnerX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {endInnerY.ToString(System.Globalization.CultureInfo.InvariantCulture)} Z";

            svg.AppendLine($"<path d=\"{path}\" fill=\"{color}\" />");

            currentAngle = endAngle;
            colorIndex++;
        }

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    public byte[] GenerateStudentReport(List<Student> students, string title)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text(text =>
                    {
                        text.Span("İzollu Dayanışma Merkezi\n").FontSize(20).Bold();
                        text.Span(title).FontSize(14);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(5);

                        column.Item().Text($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9);
                        column.Item().Text($"Toplam Öğrenci: {students.Count}").FontSize(9).Bold();

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Ad Soyad").Bold();
                                header.Cell().Element(CellStyle).Text("Üniversite").Bold();
                                header.Cell().Element(CellStyle).Text("Bölüm").Bold();
                                header.Cell().Element(CellStyle).Text("Sınıf").Bold();
                                header.Cell().Element(CellStyle).Text("Dönem").Bold();

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.Border(1).Background(Colors.Grey.Lighten2).Padding(5);
                                }
                            });

                            foreach (var student in students)
                            {
                                table.Cell().Element(CellStyle).Text(student.AdSoyad);
                                table.Cell().Element(CellStyle).Text(student.Universite ?? "-");
                                table.Cell().Element(CellStyle).Text(student.Bolum ?? "-");
                                table.Cell().Element(CellStyle).Text(student.Sinif.ToString());
                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5);
                                }
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateMeetingPresentation(
        int totalStudents,
        int activeScholarships,
        decimal totalAmount,
        Dictionary<string, decimal> sectorDistribution,
        Dictionary<string, int> universityDistribution)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                // Kapak sayfası
                page.Content()
                    .AlignCenter()
                    .AlignMiddle()
                    .Column(column =>
                    {
                        column.Item().Text("İzollu Dayanışma Merkezi").FontSize(36).Bold();
                        column.Item().PaddingTop(20).Text("Burs Programı Toplantı Sunumu").FontSize(24);
                        column.Item().PaddingTop(40).Text($"{DateTime.Now:MMMM yyyy}").FontSize(18);
                    });
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(14));

                page.Header()
                    .Text("Özet Bilgiler").FontSize(24).Bold();

                page.Content()
                    .PaddingTop(2, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Border(2).BorderColor(Colors.Blue.Medium).Padding(20)
                                .Column(col =>
                                {
                                    col.Item().Text("Toplam Öğrenci").FontSize(16);
                                    col.Item().PaddingTop(10).Text(totalStudents.ToString()).FontSize(32).Bold();
                                });

                            row.RelativeItem().Border(2).BorderColor(Colors.Green.Medium).Padding(20)
                                .Column(col =>
                                {
                                    col.Item().Text("Aktif Burs Alan").FontSize(16);
                                    col.Item().PaddingTop(10).Text(activeScholarships.ToString()).FontSize(32).Bold();
                                });

                            row.RelativeItem().Border(2).BorderColor(Colors.Orange.Medium).Padding(20)
                                .Column(col =>
                                {
                                    col.Item().Text("Toplam Aylık Burs").FontSize(16);
                                    col.Item().PaddingTop(10).Text($"{totalAmount:N0} ₺").FontSize(32).Bold();
                                });
                        });

                        if (sectorDistribution.Any())
                        {
                            column.Item().PaddingTop(20).Text("Sektör Bazlı Dağılım").FontSize(18).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                foreach (var item in sectorDistribution.OrderByDescending(x => x.Value))
                                {
                                    table.Cell().Padding(5).Text(item.Key);
                                    table.Cell().Padding(5).AlignRight().Text($"{item.Value:N0} ₺");
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateStudentsPdfReport(List<Student> students)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("İzollu Dayanışma Merkezi\n").FontSize(18).Bold().FontColor(Colors.Orange.Darken2);
                        text.Span("Öğrenci Listesi Raporu\n").FontSize(14).FontColor(Colors.Grey.Darken1);
                        text.Span($"{DateTime.Now:dd MMMM yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Toplam Öğrenci: {students.Count}").FontSize(10).Bold();
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Ad Soyad").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Üniversite").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Bölüm").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Sınıf").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Burs").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Telefon").Bold().FontSize(8);
                            });

                            foreach (var student in students)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.AdSoyad).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Universite ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Bolum ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Sinif?.ToString() ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.AktifBursMu ? "✓" : "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Telefon ?? "-").FontSize(8);
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateGraduatedStudentsPdf(List<Student> students)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("İzollu Dayanışma Merkezi\n").FontSize(18).Bold().FontColor(Colors.Green.Darken2);
                        text.Span("Mezun Öğrenci Listesi Raporu\n").FontSize(14).FontColor(Colors.Grey.Darken1);
                        text.Span($"{DateTime.Now:dd MMMM yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Toplam Mezun: {students.Count}").FontSize(10).Bold();
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Ad Soyad
                                columns.RelativeColumn(2); // Meslek
                                columns.RelativeColumn(2); // Firma
                                columns.RelativeColumn(2); // Üniversite
                                columns.RelativeColumn(2); // Telefon
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Ad Soyad").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Meslek").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Firma").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Üniversite").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Telefon").Bold().FontSize(8);
                            });

                            foreach (var student in students)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.AdSoyad).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Meslek ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Firma ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Universite ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(student.Telefon ?? "-").FontSize(8);
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateMembersPdfReport(List<Member> members)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("İzollu Dayanışma Merkezi\n").FontSize(18).Bold().FontColor(Colors.Orange.Darken2);
                        text.Span("Üye Listesi Raporu\n").FontSize(14).FontColor(Colors.Grey.Darken1);
                        text.Span($"{DateTime.Now:dd MMMM yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Toplam Üye: {members.Count}").FontSize(10).Bold();
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Ad Soyad").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Rol").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Telefon").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Email").Bold().FontSize(8);
                            });

                            foreach (var member in members)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(member.AdSoyad).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(member.UyelikTuru ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(member.Telefon ?? "-").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(member.Email ?? "-").FontSize(8);
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateComprehensiveReport(
        int totalStudents, 
        int activeScholarships, 
        int graduatedCount,
        int totalDonors,
        int totalMembers,
        decimal totalPaidScholarship,  // Changed from totalAmount - now shows actual paid amount
        string activePeriod,
        decimal periodBursTutari,
        int pledgedCount,
        int realizedCount,
        int totalAidsCount,
        int villageCount,
        Dictionary<string, int> universityData,
        List<MemberScholarshipDetail> memberScholarshipDetails,
        Dictionary<string, int> genderDistribution,
        Dictionary<string, int> malatyaLocationDistribution,
        List<MemberScholarshipDetail> topDonors,
        List<MemberScholarshipDetail> unfulfilledCommitments,
        Dictionary<string, int> villageAidData,
        List<RoleBasedScholarshipPdf> roleBasedScholarshipData)
    {
        // Enable debugging to see layout issues
        QuestPDF.Settings.EnableDebugging = true;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("İzollu Dayanışma Merkezi\n").FontSize(22).Bold().FontColor(Colors.Orange.Darken2);
                        text.Span("Kapsamlı Analiz Raporu\n").FontSize(16).FontColor(Colors.Grey.Darken1);
                        text.Span($"Rapor Tarihi: {DateTime.Now:dd MMMM yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Genel İstatistikler - Modern Dashboard Style (ShowEntire prevents page breaks)
                        column.Item().ShowEntire().Background(Colors.Orange.Lighten5).Padding(10).Column(container =>
                        {
                            container.Item().PaddingBottom(6).Text("GENEL İSTATİSTİKLER").FontSize(14).Bold().FontColor(Colors.Orange.Darken3);
                            
                            // Student & Member Counts Section
                            container.Item().PaddingBottom(4).Text("Öğrenci ve Üye Sayıları").FontSize(10).FontColor(Colors.Grey.Darken2);
                            container.Item().PaddingBottom(6).Row(cardRow =>
                            {
                                // Card: Total Students
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(totalStudents.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Toplam Öğrenci").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Scholarship Students
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(activeScholarships.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Burs Alan").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Graduates
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(graduatedCount.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Mezun").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Members
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(totalMembers.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Üye Sayısı").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                            });
                            
                            // Financial Section
                            container.Item().PaddingBottom(4).Text("Finansal Bilgiler").FontSize(10).FontColor(Colors.Grey.Darken2);
                            container.Item().PaddingBottom(6).Row(cardRow =>
                            {
                                // Card: Period Scholarship Amount
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text($"{periodBursTutari:N0} ₺").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Taahhüt Edilen").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Total Paid Scholarship
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text($"{totalPaidScholarship:N0} ₺").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Toplam Ödenen Burs").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Pledged Count
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(pledgedCount.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Taahhüt Sayısı").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Realized Count
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(realizedCount.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Gerçekleşen").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                            });
                            
                            // Additional Info Section
                            container.Item().PaddingBottom(4).Text("Diğer Bilgiler").FontSize(10).FontColor(Colors.Grey.Darken2);
                            container.Item().Row(cardRow =>
                            {
                                // Card: Donors
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(totalDonors.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("İş Adamı").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Total Aids
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(totalAidsCount.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Yardım Sayısı").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Villages
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(villageCount.ToString()).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Köy Sayısı").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                                
                                // Card: Active Period
                                cardRow.RelativeItem(1).Padding(2).Column(cardCol =>
                                {
                                    cardCol.Item().Height(55).Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).AlignCenter().AlignMiddle().Column(content =>
                                        {
                                            content.Item().AlignCenter().Text(activePeriod).FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                                            content.Item().AlignCenter().PaddingTop(2).Text("Aktif Dönem").FontSize(8).FontColor(Colors.Grey.Medium);
                                        });
                                });
                            });
                        });


                        // Burs Taahhüt Özeti
                        var totalPledged = memberScholarshipDetails.Sum(m => m.PledgedCount);
                        var totalRealized = memberScholarshipDetails.Sum(m => m.RealizedCount);
                        var realizationRate = totalPledged > 0 ? (totalRealized * 100.0 / totalPledged) : 0;
                        
                        column.Item().PaddingTop(10).Background(Colors.Purple.Lighten4).Padding(10).Column(col =>
                        {
                            col.Item().Text("BURS TAAHHÜT ÖZETİ").FontSize(14).Bold().FontColor(Colors.Purple.Darken3);
                            col.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor(Colors.Purple.Medium).Padding(10).Column(c =>
                                {
                                    c.Item().Text("Toplam Taahhüt").FontSize(10);
                                    c.Item().Text($"{totalPledged} Burs").FontSize(16).Bold().FontColor(Colors.Purple.Darken2);
                                });
                                row.RelativeItem().Border(1).BorderColor(Colors.Green.Medium).Padding(10).Column(c =>
                                {
                                    c.Item().Text("Gerçekleşen").FontSize(10);
                                    c.Item().Text($"{totalRealized} Burs").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                                });
                                row.RelativeItem().Border(1).BorderColor(Colors.Blue.Medium).Padding(10).Column(c =>
                                {
                                    c.Item().Text("Gerçekleşme Oranı").FontSize(10);
                                    c.Item().Text($"%{realizationRate:F1}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                                });
                            });
                        });

                        // Cinsiyet ve Konum Dağılımı - Side-by-Side Layout
                        column.Item().PaddingTop(10).Row(chartsRow =>
                        {
                            // Cinsiyet Dağılımı - Left Side
                            chartsRow.RelativeItem(0.48f).Column(genderColumn =>
                            {
                                if (genderDistribution != null && genderDistribution.Any())
                                {
                                    var genderTotal = genderDistribution.Values.Sum();
                                    
                                    // Define colors for gender
                                    var genderColors = new List<string>();
                                    foreach (var item in genderDistribution)
                                    {
                                        genderColors.Add(item.Key == "Kadın" ? "#ec4899" : item.Key == "Erkek" ? "#3b82f6" : "#9ca3af");
                                    }

                                    genderColumn.Item().Text("ÖĞRENCİLERİN CİNSİYET DAĞILIMI").FontSize(12).Bold();
                                    genderColumn.Item().PaddingTop(5).Row(chartRow =>
                                    {
                                        // Donut Chart
                                        var genderSvg = GenerateDonutChartSvg(genderDistribution, genderColors, 120, 0.65);
                                        chartRow.ConstantItem(130).AlignCenter().Column(chartCol =>
                                        {
                                            chartCol.Item().AlignCenter().Width(120).Height(120).Svg(genderSvg);
                                        });
                                        
                                        // Legend with colored boxes
                                        chartRow.RelativeItem().PaddingLeft(15).AlignMiddle().Column(legendCol =>
                                        {
                                            foreach (var item in genderDistribution)
                                            {
                                                var percentage = genderTotal > 0 ? (item.Value * 100.0 / genderTotal) : 0;
                                                var colorHex = item.Key == "Kadın" ? "#ec4899" : 
                                                               item.Key == "Erkek" ? "#3b82f6" : "#9ca3af";
                                                
                                                legendCol.Item().PaddingBottom(5).Row(itemRow =>
                                                {
                                                    itemRow.ConstantItem(14).Height(14).Background(colorHex);
                                                    itemRow.RelativeItem().PaddingLeft(8).Text($"{item.Key}: {item.Value} (%{percentage:F1})").FontSize(10);
                                                });
                                            }
                                        });
                                    });
                                }
                            });

                            // Spacing between charts
                            chartsRow.RelativeItem(0.04f);

                            // Malatya Konum Dağılımı - Right Side
                            chartsRow.RelativeItem(0.48f).Column(locationColumn =>
                            {
                                if (malatyaLocationDistribution != null && malatyaLocationDistribution.Any())
                                {
                                    var locationTotal = malatyaLocationDistribution.Values.Sum();
                                    
                                    // Define colors for location
                                    var locationColors = new List<string>();
                                    foreach (var item in malatyaLocationDistribution)
                                    {
                                        locationColors.Add(item.Key == "Malatya İçi" ? "#f59e0b" : "#ef4444");
                                    }

                                    locationColumn.Item().Text("MALATYA KONUMUNA GÖRE DAĞILIM").FontSize(12).Bold();
                                    locationColumn.Item().PaddingTop(5).Row(chartRow =>
                                    {
                                        // Donut Chart
                                        var locationSvg = GenerateDonutChartSvg(malatyaLocationDistribution, locationColors, 120, 0.65);
                                        chartRow.ConstantItem(130).AlignCenter().Column(chartCol =>
                                        {
                                            chartCol.Item().AlignCenter().Width(120).Height(120).Svg(locationSvg);
                                        });
                                        
                                        // Legend with colored boxes
                                        chartRow.RelativeItem().PaddingLeft(15).AlignMiddle().Column(legendCol =>
                                        {
                                            foreach (var item in malatyaLocationDistribution)
                                            {
                                                var percentage = locationTotal > 0 ? (item.Value * 100.0 / locationTotal) : 0;
                                                var colorHex = item.Key == "Malatya İçi" ? "#f59e0b" : "#ef4444";
                                                
                                                legendCol.Item().PaddingBottom(5).Row(itemRow =>
                                                {
                                                    itemRow.ConstantItem(14).Height(14).Background(colorHex);
                                                    itemRow.RelativeItem().PaddingLeft(8).Text($"{item.Key}: {item.Value} (%{percentage:F1})").FontSize(10);
                                                });
                                            }
                                        });
                                    });
                                }
                            });
                        });

                        // Üniversite Dağılımı
                        if (universityData.Any())
                        {
                            column.Item().PageBreak();
                            column.Item().PaddingTop(10).Text("Üniversiteye Göre Öğrenci Dağılımı").FontSize(12).Bold();
                            column.Item().PaddingTop(5).Column(uniCol =>
                            {
                                var maxCount = universityData.Take(10).Max(x => x.Value);
                                foreach (var item in universityData.Take(10))
                                {
                                    uniCol.Item().PaddingBottom(8).Row(barRow =>
                                    {
                                        // University name - fixed width
                                        barRow.ConstantItem(150).AlignRight().PaddingRight(10).Text(item.Key).FontSize(9);
                                        
                                        // Bar and count
                                        barRow.RelativeItem().Row(barContentRow =>
                                        {
                                            // Colored bar proportional to count
                                            var barWidth = maxCount > 0 ? (int)((item.Value * 100.0) / maxCount) : 1;
                                            barContentRow.RelativeItem(barWidth).Height(20).Background("#a78bfa");
                                            
                                            // Empty space
                                            if (barWidth < 100)
                                                barContentRow.RelativeItem(100 - barWidth).Height(20);
                                            
                                            // Count label at the end
                                            barContentRow.ConstantItem(30).AlignLeft().PaddingLeft(5).Text(item.Value.ToString()).FontSize(10).Bold();
                                        });
                                    });
                                }
                            });
                        }

                        // Köylere Göre Yardım Dağılımı
                        if (villageAidData != null && villageAidData.Any())
                        {
                            column.Item().PageBreak();
                            column.Item().Background(Colors.Teal.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("KÖYLERE GÖRE YARDIM DAĞILIMI").FontSize(12).Bold().FontColor(Colors.Teal.Darken3);
                            });
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Köy").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Yardım Sayısı").Bold();
                                });

                                foreach (var item in villageAidData.OrderByDescending(v => v.Value))
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Key);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.Value}");
                                }
                            });
                        }

                        // Rol Bazlı Burs Dağılımı - Comparison Table with Progress Bars
                        if (roleBasedScholarshipData != null && roleBasedScholarshipData.Any())
                        {
                            column.Item().PaddingTop(15).Background(Colors.Indigo.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("ROL BAZLI BURS DAĞILIMI").FontSize(12).Bold().FontColor(Colors.Indigo.Darken3);
                            });
                            
                            var roleColors = new[] { "#8b5cf6", "#ec4899", "#f59e0b", "#10b981", "#3b82f6", "#ef4444" };
                            
                            // Comparison Table
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2.5f);  // Role Name
                                    columns.RelativeColumn(2);     // Committed Amount
                                    columns.RelativeColumn(2);     // Realized Amount
                                    columns.RelativeColumn(1.5f);  // Completion Rate
                                    columns.RelativeColumn(3);     // Progress Bar
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Indigo.Lighten3).Padding(6).Text("Rol").Bold().FontSize(9);
                                    header.Cell().Background(Colors.Indigo.Lighten3).Padding(6).AlignRight().Text("Taahhüt Edilen").Bold().FontSize(9);
                                    header.Cell().Background(Colors.Indigo.Lighten3).Padding(6).AlignRight().Text("Gerçekleşen").Bold().FontSize(9);
                                    header.Cell().Background(Colors.Indigo.Lighten3).Padding(6).AlignCenter().Text("Oran").Bold().FontSize(9);
                                    header.Cell().Background(Colors.Indigo.Lighten3).Padding(6).Text("İlerleme").Bold().FontSize(9);
                                });

                                int colorIndex = 0;
                                foreach (var item in roleBasedScholarshipData)
                                {
                                    var completionRate = item.PledgedCount > 0 ? (item.RealizedCount * 100.0 / item.PledgedCount) : 0;
                                    var colorHex = roleColors[colorIndex % roleColors.Length];
                                    colorIndex++;
                                    
                                    // Role Name with color indicator
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Row(r =>
                                    {
                                        r.ConstantItem(8).Height(8).AlignMiddle().Background(colorHex);
                                        r.RelativeItem().PaddingLeft(5).AlignMiddle().Text(item.Role).FontSize(9);
                                    });
                                    
                                    // Committed Amount (if available, else show count)
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().AlignMiddle()
                                        .Text(item.CommittedAmount > 0 ? $"{item.CommittedAmount:N0} ₺" : $"{item.PledgedCount} burs").FontSize(9);
                                    
                                    // Realized Amount (if available, else show count)
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().AlignMiddle()
                                        .Text(item.RealizedAmount > 0 ? $"{item.RealizedAmount:N0} ₺" : $"{item.RealizedCount} burs").FontSize(9).FontColor(Colors.Green.Darken2);
                                    
                                    // Completion Rate
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().AlignMiddle()
                                        .Text($"%{completionRate:F0}").FontSize(9).Bold().FontColor(completionRate >= 80 ? Colors.Green.Darken2 : completionRate >= 50 ? Colors.Orange.Darken2 : Colors.Red.Medium);
                                    
                                    // Progress Bar
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(progressCol =>
                                    {
                                        progressCol.Item().Height(12).Background(Colors.Grey.Lighten3).Row(progressRow =>
                                        {
                                            var barWidth = (int)Math.Max(1, Math.Min(100, completionRate));
                                            if (barWidth > 0)
                                            {
                                                progressRow.RelativeItem(barWidth).Background(colorHex);
                                            }
                                            if (barWidth < 100)
                                            {
                                                progressRow.RelativeItem(100 - barWidth);
                                            }
                                        });
                                    });
                                }
                                
                                // Summary row
                                var totalPledgedSum = roleBasedScholarshipData.Sum(r => r.PledgedCount);
                                var totalRealizedSum = roleBasedScholarshipData.Sum(r => r.RealizedCount);
                                var totalCommittedAmountSum = roleBasedScholarshipData.Sum(r => r.CommittedAmount);
                                var totalRealizedAmountSum = roleBasedScholarshipData.Sum(r => r.RealizedAmount);
                                var totalRate = totalPledgedSum > 0 ? (totalRealizedSum * 100.0 / totalPledgedSum) : 0;
                                
                                table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("TOPLAM").Bold().FontSize(9);
                                table.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                    .Text(totalCommittedAmountSum > 0 ? $"{totalCommittedAmountSum:N0} ₺" : $"{totalPledgedSum} burs").Bold().FontSize(9);
                                table.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                    .Text(totalRealizedAmountSum > 0 ? $"{totalRealizedAmountSum:N0} ₺" : $"{totalRealizedSum} burs").Bold().FontSize(9).FontColor(Colors.Green.Darken2);
                                table.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignCenter()
                                    .Text($"%{totalRate:F0}").Bold().FontSize(9);
                                table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Column(progressCol =>
                                {
                                    progressCol.Item().Height(12).Background(Colors.Grey.Lighten1).Row(progressRow =>
                                    {
                                        var barWidth = (int)Math.Max(1, Math.Min(100, totalRate));
                                        if (barWidth > 0)
                                        {
                                            progressRow.RelativeItem(barWidth).Background(Colors.Indigo.Medium);
                                        }
                                        if (barWidth < 100)
                                        {
                                            progressRow.RelativeItem(100 - barWidth);
                                        }
                                    });
                                });
                            });
                        }

                        // En Çok Burs Verenler
                        if (topDonors != null && topDonors.Any())
                        {
                            column.Item().PageBreak();
                            column.Item().Background(Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("EN ÇOK BURS VERENLER (İLK 10)").FontSize(12).Bold().FontColor(Colors.Green.Darken3);
                            });
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Sıra").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Üye Adı").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Taahhüt").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Gerçekleşen").Bold();
                                });

                                int rank = 1;
                                foreach (var item in topDonors.Take(10))
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{rank++}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.MemberName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.PledgedCount}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.RealizedCount}").FontColor(Colors.Green.Darken2).Bold();
                                }
                            });
                        }

                        // Taahhüdünü Yerine Getirmeyenler
                        if (unfulfilledCommitments != null && unfulfilledCommitments.Any())
                        {
                            column.Item().PaddingTop(15).Background(Colors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("TAAHHÜDÜNÜ YERİNE GETİRMEYENLER").FontSize(12).Bold().FontColor(Colors.Red.Darken3);
                            });
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Üye Adı").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Taahhüt").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Gerçekleşen").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Eksik").Bold();
                                });

                                foreach (var item in unfulfilledCommitments)
                                {
                                    var deficit = item.PledgedCount - item.RealizedCount;
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.MemberName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.PledgedCount}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.RealizedCount}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{deficit}").FontColor(Colors.Red.Darken2).Bold();
                                }
                            });
                        }

                        // Üyelerin Verdiği Burs Dağılımı
                        if (memberScholarshipDetails.Any())
                        {
                            column.Item().PageBreak();
                            column.Item().Text("TÜM ÜYELERİN BURS DAĞILIMI").FontSize(12).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Üye Adı").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Taahhüt Edilen").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Gerçekleşen").Bold();
                                });

                                foreach (var item in memberScholarshipDetails.OrderByDescending(x => x.PledgedCount))
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.MemberName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.PledgedCount}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.RealizedCount}");
                                }
                            });
                        }

                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("İzollu Dayanışma Merkezi - ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9);
                        text.Span(" / ").FontSize(9);
                        text.TotalPages().FontSize(9);
                    });
            });
        });

        return document.GeneratePdf();
    }
}