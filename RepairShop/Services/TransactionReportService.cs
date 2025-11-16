using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using RepairShop.Models;

namespace RepairShop.Services
{
    public class TransactionReportService
    {
        public byte[] GenerateTransactionReportPdf(TransactionHeader transaction)
        {
            GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();

            // Create document
            Document document = CreateDocument(transaction);

            // Render to PDF
            var pdfRenderer = new PdfDocumentRenderer()
            {
                Document = document
            };
            pdfRenderer.RenderDocument();

            using var stream = new MemoryStream();
            pdfRenderer.Save(stream, false);
            return stream.ToArray();
        }

        private Document CreateDocument(TransactionHeader transaction)
        {
            Document document = new()
            {
                Info =
                {
                    Title = $"Transaction Report - {transaction.Id:00000}",
                    Author = "RepairShop System"
                }
            };

            DefineStyles(document);

            Section section = document.AddSection();
            section.PageSetup.Orientation = Orientation.Portrait;
            section.PageSetup.PageFormat = PageFormat.A4;

            AddHeader(section, transaction);
            AddTransactionSummary(section, transaction);
            AddFooter(section);

            return document;
        }

        private void DefineStyles(Document document)
        {
            var titleStyle = document.Styles.AddStyle("Title", "Normal");
            titleStyle.Font.Size = 16;
            titleStyle.Font.Bold = true;
            titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            titleStyle.ParagraphFormat.SpaceAfter = "1cm";

            var headerStyle = document.Styles.AddStyle("Header", "Normal");
            headerStyle.Font.Size = 12;
            headerStyle.Font.Bold = true;
            headerStyle.ParagraphFormat.SpaceBefore = "0.5cm";
            headerStyle.ParagraphFormat.SpaceAfter = "0.2cm";

            var normal = document.Styles["Normal"];
            normal.Font.Size = 10;

            var tableHeader = document.Styles.AddStyle("TableHeader", "Normal");
            tableHeader.Font.Bold = true;
            tableHeader.Font.Size = 10;

            var tableContent = document.Styles.AddStyle("TableContent", "Normal");
            tableContent.Font.Size = 10;
        }

        private void AddHeader(Section section, TransactionHeader transaction)
        {
            var header = section.AddParagraph();
            header.Style = "Title";
            header.AddFormattedText("REPAIRSHOP", TextFormat.Bold);
            header.AddLineBreak();
            header.AddFormattedText("Transaction Completion Report", TextFormat.Bold);
            header.AddLineBreak();
            header.AddText($"Transaction ID: TR-{transaction.Id:00000}");
            header.AddLineBreak();
            header.AddFormattedText($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm tt}", TextFormat.Italic);

            var line = section.AddParagraph();
            line.Format.Borders.Bottom.Color = Colors.Black;
            line.Format.Borders.Bottom.Width = 1;
            line.Format.SpaceAfter = "0.5cm";
        }

        private void AddTransactionSummary(Section section, TransactionHeader transaction)
        {
            var du = transaction.DefectiveUnit;
            var sn = du?.SerialNumber;

            var header = section.AddParagraph("TRANSACTION SUMMARY");
            header.Style = "Header";

            var table = CreateBaseTable(section);

            AddTableRow(table, "Client:", transaction.DefectiveUnit?.SerialNumber?.Client?.Name ?? "N/A");
            AddTableRow(table, "Branch:", transaction.DefectiveUnit?.SerialNumber?.Client?.Branch ?? "N/A");
            AddTableRow(table, "Handled By:", transaction.User?.UserName ?? "N/A");

            if (sn != null)
            {
                AddTableRow(table, "Serial Number:", sn.Value);
                AddTableRow(table, "Model:", sn.Model?.Name ?? "N/A");
            }

            AddTableRow(table, "Defective Unit Status:", "Fixed");
            AddTableRow(table, "Delivery Date:",
                transaction.DeliveredDate?.ToString("dd/MM/yyyy HH:mm tt") ?? "Pending Delivery");
        }

        private void AddFooter(Section section)
        {
            section.AddParagraph().AddLineBreak();

            var line = section.AddParagraph();
            line.Format.Borders.Top.Color = Colors.Black;
            line.Format.Borders.Top.Width = 1;
            line.Format.SpaceBefore = "0.5cm";

            var footer = section.AddParagraph();
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.AddFormattedText("RepairShop - Thank you for your trust!", TextFormat.Italic);
        }

        private Table CreateBaseTable(Section section)
        {
            var table = section.AddTable();
            table.Borders.Width = 0.25;
            table.Borders.Color = Colors.Black;

            table.AddColumn("5cm").Format.Alignment = ParagraphAlignment.Left;
            table.AddColumn("10cm").Format.Alignment = ParagraphAlignment.Left;

            return table;
        }

        private void AddTableRow(Table table, string label, string value)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(label).Style = "TableHeader";
            row.Cells[1].AddParagraph(value ?? "N/A").Style = "TableContent";
        }
    }
}
