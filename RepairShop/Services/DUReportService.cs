using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using RepairShop.Models;

namespace RepairShop.Services
{
    public class DUReportService
    {
        public byte[] GenerateDUReportPdf(DefectiveUnit du)
        {
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
            }

            // Create MigraDoc document
            Document document = CreateDocument(du);

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

        private Document CreateDocument(DefectiveUnit du)
        {
            Document document = new Document
            {
                Info =
                {
                    Title = $"Defective Unit Report - {du.SerialNumber?.Value}",
                    Author = "RepairShop System"
                }
            };

            DefineStyles(document);

            Section section = document.AddSection();
            section.PageSetup.Orientation = Orientation.Portrait;
            section.PageSetup.PageFormat = PageFormat.A4;

            AddHeader(section, du);
            AddDefectiveUnitDetails(section, du);
            AddSerialNumberDetails(section, du);
            AddCoverageDetails(section, du);
            AddFooter(section);

            return document;
        }

        private void DefineStyles(Document document)
        {
            // Title style
            var titleStyle = document.Styles.AddStyle("Title", "Normal");
            titleStyle.Font.Size = 16;
            titleStyle.Font.Bold = true;
            titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            titleStyle.ParagraphFormat.SpaceAfter = "1cm";

            // Header style
            var headerStyle = document.Styles.AddStyle("Header", "Normal");
            headerStyle.Font.Size = 12;
            headerStyle.Font.Bold = true;
            headerStyle.ParagraphFormat.SpaceBefore = "0.5cm";
            headerStyle.ParagraphFormat.SpaceAfter = "0.2cm";

            // Normal style
            var normalStyle = document.Styles["Normal"];
            normalStyle.Font.Size = 10;
            normalStyle.ParagraphFormat.SpaceAfter = "0.1cm";

            // Table header style
            var tableHeader = document.Styles.AddStyle("TableHeader", "Normal");
            tableHeader.Font.Size = 10;
            tableHeader.Font.Bold = true;
            tableHeader.ParagraphFormat.Alignment = ParagraphAlignment.Left;

            // Table content style
            var tableContent = document.Styles.AddStyle("TableContent", "Normal");
            tableContent.Font.Size = 9;
            tableContent.ParagraphFormat.Alignment = ParagraphAlignment.Left;
        }

        private void AddHeader(Section section, DefectiveUnit du)
        {
            var header = section.AddParagraph();
            header.Format.Alignment = ParagraphAlignment.Center;
            header.Format.SpaceAfter = "0.5cm";
            header.Style = "Title";

            header.AddFormattedText("REPAIRSHOP", TextFormat.Bold);
            header.AddLineBreak();
            header.AddFormattedText("Defective Unit Report", TextFormat.Bold);
            header.AddLineBreak();
            header.AddText($"Report ID: DU-{du.Id:00000}");
            header.AddLineBreak();
            header.AddFormattedText($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm tt}", TextFormat.Italic);

            // Add a separator line
            var line = section.AddParagraph();
            line.AddLineBreak();
            line.Format.Borders.Bottom.Color = Colors.Black;
            line.Format.Borders.Bottom.Width = 1;
            line.Format.SpaceAfter = "0.5cm";
        }

        private void AddDefectiveUnitDetails(Section section, DefectiveUnit du)
        {
            var header = section.AddParagraph("DEFECTIVE UNIT DETAILS");
            header.Style = "Header";

            var table = CreateBaseTable(section);

            AddTableRow(table, "Reported Date:", du.ReportedDate.ToString("dd/MM/yyyy HH:mm tt"));
            AddTableRow(table, "Status:", du.Status ?? "N/A");
            AddTableRow(table, "Description:", du.Description ?? "No description provided");
            AddTableRow(table, "Has Accessories:", du.HasAccessories ? "Yes" : "No");

            if (du.HasAccessories && !string.IsNullOrWhiteSpace(du.Accessories))
                AddTableRow(table, "Accessories:", du.Accessories);

            if (du.ResolvedDate.HasValue)
                AddTableRow(table, "Resolved Date:", du.ResolvedDate.Value.ToString("dd/MM/yyyy HH:mm tt"));

            section.AddParagraph(); // Add space after
        }

        private void AddSerialNumberDetails(Section section, DefectiveUnit du)
        {
            if (du.SerialNumber == null)
                return;

            var header = section.AddParagraph("DEVICE INFORMATION");
            header.Style = "Header";

            var table = CreateBaseTable(section);

            AddTableRow(table, "Serial Number:", du.SerialNumber.Value);
            AddTableRow(table, "Model:", du.SerialNumber.Model?.Name ?? "N/A");
            AddTableRow(table, "Client:", du.SerialNumber.Client?.Name ?? "N/A");
            AddTableRow(table, "Received Date:", du.SerialNumber.ReceivedDate.ToString("dd/MM/yyyy HH:mm tt"));
        }

        private void AddCoverageDetails(Section section, DefectiveUnit du)
        {
            var header = section.AddParagraph("COVERAGE INFORMATION");
            header.Style = "Header";

            var table = CreateBaseTable(section);

            var warrantyStatus = du.SerialNumber.WarrantyId.HasValue ? "Covered by Warranty" : "Not Covered by Warranty";
            var contractStatus = du.SerialNumber.MaintenanceContractId.HasValue ? "Covered by Maintenance Contract" : "Not Covered by Maintenance Contract";

            AddTableRow(table, "Warranty:", warrantyStatus);
            AddTableRow(table, "Contract:", contractStatus);
        }

        private void AddFooter(Section section)
        {
            section.AddParagraph().AddLineBreak();

            var footerLine = section.AddParagraph();
            footerLine.Format.Borders.Top.Color = Colors.Black;
            footerLine.Format.Borders.Top.Width = 1;
            footerLine.Format.SpaceBefore = "0.5cm";

            var footer = section.AddParagraph();
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.Format.SpaceBefore = "0.5cm";
            footer.AddFormattedText("RepairShop - Quality Service Guaranteed", TextFormat.Italic);
        }

        private Table CreateBaseTable(Section section)
        {
            var table = section.AddTable();
            table.Style = "Table";
            table.Borders.Width = 0.25;
            table.Borders.Color = Colors.Black;

            table.AddColumn("4cm").Format.Alignment = ParagraphAlignment.Left;
            table.AddColumn("11cm").Format.Alignment = ParagraphAlignment.Left;

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