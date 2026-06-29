using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using RepairShop.Models;

namespace RepairShop.Services
{
    public class DeliveryNoteReportService
    {
        public byte[] GenerateDeliveryNotePdf(TransactionHeader transaction, string code)
        {
            GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();

            var document = CreateDocument(transaction, code);

            PdfDocumentRenderer renderer = new()
            {
                Document = document
            };

            renderer.RenderDocument();

            using MemoryStream ms = new();

            renderer.Save(ms, false);

            return ms.ToArray();
        }

        private Document CreateDocument(TransactionHeader transaction, string code)
        {
            Document document = new();

            document.Info.Title = $"Delivery Note {code}";

            DefineStyles(document);

            Section section = document.AddSection();

            AddHeader(section, transaction, code);

            AddClientInformation(section, transaction);

            AddDeviceInformation(section, transaction);

            AddRepairInformation(section, transaction);

            AddBrokenParts(section, transaction);

            AddSignatureSection(section);

            return document;
        }

        private void DefineStyles(Document document)
        {
            var normal = document.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 10;

            var title = document.Styles.AddStyle("Title2", "Normal");
            title.Font.Size = 20;
            title.Font.Bold = true;
            title.ParagraphFormat.Alignment = ParagraphAlignment.Center;

            var heading = document.Styles.AddStyle("Heading2", "Normal");
            heading.Font.Bold = true;
            heading.Font.Size = 12;
            heading.Font.Color = Colors.DarkBlue;
        }

        private void AddHeader(Section section, TransactionHeader transaction, string code)
        {
            var company = section.AddParagraph();
            company.Format.Alignment = ParagraphAlignment.Center;
            company.Format.SpaceAfter = "0.2cm";
            company.Format.Font.Size = 20;
            company.Format.Font.Bold = true;
            company.AddText("REPAIRSHOP");

            var title = section.AddParagraph();
            title.Format.Alignment = ParagraphAlignment.Center;
            title.Format.SpaceAfter = "0.2cm";
            title.Format.Font.Size = 16;
            title.Format.Font.Bold = true;
            title.AddText("DELIVERY NOTE");

            var codeParagraph = section.AddParagraph();
            codeParagraph.Format.Alignment = ParagraphAlignment.Center;
            codeParagraph.Format.SpaceAfter = "0.6cm";
            codeParagraph.Format.Borders.Width = 0.75;
            codeParagraph.Format.Borders.Distance = "3mm";
            codeParagraph.Format.Font.Size = 13;
            codeParagraph.Format.Font.Bold = true;
            codeParagraph.AddText(code);
        }

        private void AddClientInformation(Section section, TransactionHeader transaction)
        {
            Table table = section.AddTable();

            table.Borders.Width = 0.5;

            table.AddColumn("4cm");
            table.AddColumn("12cm");

            var client =
                transaction.DefectiveUnit.SerialNumber.Client.ParentClient
                ?? transaction.DefectiveUnit.SerialNumber.Client;

            var branch =
                transaction.DefectiveUnit.SerialNumber.Client.ParentClient != null
                ? transaction.DefectiveUnit.SerialNumber.Client.Name
                : "N/A";

            AddInfoRow(table, "Client", client.Name);
            AddInfoRow(table, "Branch", branch);
            AddInfoRow(table, "Delivery Date",
                transaction.DeliveredDate?.ToString("dd/MM/yyyy hh:mm tt") ?? "");

            AddInfoRow(table,
                "Technician",
                transaction.User?.UserName ?? "");

            section.AddParagraph();
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            Row row = table.AddRow();

            row.Cells[0].Shading.Color = Colors.LightGray;

            row.Cells[0].AddParagraph(label);

            row.Cells[1].AddParagraph(value);
        }

        private void AddDeviceInformation(Section section, TransactionHeader transaction)
        {
            var heading = section.AddParagraph();
            heading.Format.Font.Bold = true;
            heading.Format.SpaceAfter = "0.3cm";
            heading.AddText("Device Information");

            Table table = section.AddTable();

            table.Borders.Width = 0.5;

            table.AddColumn("4cm");
            table.AddColumn("12cm");

            AddInfoRow(table,
                "Model",
                transaction.DefectiveUnit.SerialNumber.Model.Name);

            AddInfoRow(table,
                "Serial Number",
                transaction.DefectiveUnit.SerialNumber.Value);

            AddInfoRow(table,
                "Description",
                transaction.DefectiveUnit.Description ?? "");

            section.AddParagraph();
        }

        private void AddRepairInformation(Section section, TransactionHeader transaction)
        {
            var heading = section.AddParagraph();
            heading.Format.Font.Bold = true;
            heading.Format.SpaceAfter = "0.3cm";
            heading.AddText("Repair Summary");

            Table table = section.AddTable();

            table.Borders.Width = 0.5;

            table.AddColumn("4cm");
            table.AddColumn("12cm");

            AddInfoRow(table,
                "Transaction",
                transaction.Code ?? transaction.Id.ToString());

            AddInfoRow(table,
                "Status",
                transaction.Status);

            AddInfoRow(table,
                "Labor Fees",
                $"${transaction.LaborFees ?? 0:0.00}");

            section.AddParagraph();
        }

        private void AddBrokenParts(Section section, TransactionHeader transaction)
        {
            var heading = section.AddParagraph();
            heading.Format.Font.Bold = true;
            heading.Format.SpaceAfter = "0.3cm";
            heading.AddText("Repair Details");

            Table table = section.AddTable();

            table.Borders.Width = 0.5;

            table.AddColumn("1cm");
            table.AddColumn("8cm");
            table.AddColumn("7cm");

            Row header = table.AddRow();

            header.Shading.Color = Colors.DarkBlue;
            header.Format.Font.Bold = true;
            header.Format.Font.Color = Colors.White;

            header.Cells[0].AddParagraph("#");
            header.Cells[1].AddParagraph("Part");
            header.Cells[2].AddParagraph("Result");

            int index = 1;

            foreach (var part in transaction.BrokenParts.Where(x => x.IsActive))
            {
                Row row = table.AddRow();

                if (index % 2 == 0)
                    row.Shading.Color = Colors.LightYellow;

                row.Cells[0].AddParagraph(index.ToString());

                row.Cells[1].AddParagraph(
                    part.Part?.Name ??
                    part.BrokenPartName);

                row.Cells[2].AddParagraph(part.Status);

                index++;
            }

            if (index == 1)
            {
                Row row = table.AddRow();
                row.Cells[0].MergeRight = 2;
                row.Cells[0].AddParagraph("No repair parts were registered.");
                row.Cells[0].Format.Alignment = ParagraphAlignment.Center;
            }

            section.AddParagraph();
        }

        private void AddSignatureSection(Section section)
        {
            Table table = section.AddTable();

            table.AddColumn("8cm");
            table.AddColumn("8cm");

            Row row1 = table.AddRow();

            row1.Cells[0].AddParagraph("Customer Signature");
            row1.Cells[1].AddParagraph("Technician Signature");

            Row row2 = table.AddRow();

            row2.Height = "2cm";

            row2.Cells[0].Borders.Top.Width = 0.75;
            row2.Cells[1].Borders.Top.Width = 0.75;

            section.AddParagraph();

            var thanks = section.AddParagraph();

            thanks.Format.Alignment = ParagraphAlignment.Center;
            thanks.Format.Font.Italic = true;
            thanks.Format.SpaceBefore = "0.5cm";

            thanks.AddText("Thank you for choosing RepairShop");
        }
    }
}