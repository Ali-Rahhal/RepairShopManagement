using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using RepairShop.Models;

namespace RepairShop.Services
{
    public class ReceptionNoteReportService
    {
        public byte[] GenerateReceptionNotePdf(ReceptionNote note)
        {
            GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();

            Document document = CreateDocument(note);

            PdfDocumentRenderer renderer = new()
            {
                Document = document
            };

            renderer.RenderDocument();

            using MemoryStream ms = new();

            renderer.Save(ms, false);

            return ms.ToArray();
        }

        private Document CreateDocument(ReceptionNote note)
        {
            Document document = new();

            document.Info.Title = note.Code;

            DefineStyles(document);

            Section section = document.AddSection();

            AddHeader(section, note);

            AddClientInfo(section, note);

            AddItems(section, note);

            AddFooter(section);

            return document;
        }

        private void DefineStyles(Document document)
        {
            var normal = document.Styles["Normal"];

            normal.Font.Name = "Arial";
            normal.Font.Size = 10;

            var title = document.Styles.AddStyle("Title2", "Normal");

            title.Font.Size = 18;
            title.Font.Bold = true;
            title.ParagraphFormat.Alignment = ParagraphAlignment.Center;

            var header = document.Styles.AddStyle("Header2", "Normal");

            header.Font.Bold = true;
        }

        private void AddHeader(Section section, ReceptionNote note)
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
            title.AddText("RECEPTION NOTE");

            var code = section.AddParagraph();
            code.Format.Alignment = ParagraphAlignment.Center;
            code.Format.SpaceAfter = "0.6cm";
            code.Format.Borders.Width = 0.75;
            code.Format.Borders.Distance = "3mm";
            code.Format.Font.Size = 13;
            code.Format.Font.Bold = true;
            code.AddText(note.Code);
        }

        private void AddClientInfo(Section section, ReceptionNote note)
        {
            Table table = section.AddTable();

            table.Borders.Width = 0.5;

            table.AddColumn("4cm");
            table.AddColumn("12cm");

            AddInfoRow(table, "Client", note.Client.Name);
            AddInfoRow(table, "Reception Date",
                note.CreatedDate.ToString("dd/MM/yyyy hh:mm tt"));
            AddInfoRow(table, "Total Devices",
                note.Items.Count.ToString());

            section.AddParagraph();
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            Row row = table.AddRow();

            row.Cells[0].Shading.Color = Colors.LightGray;
            row.Cells[0].AddParagraph(label);

            row.Cells[1].AddParagraph(value);
        }

        private void AddItems(Section section, ReceptionNote note)
        {
            var heading = section.AddParagraph();
            heading.Format.Font.Bold = true;
            heading.Format.SpaceAfter = "0.3cm";
            heading.AddText("Received Devices");

            Table table = section.AddTable();

            table.Borders.Width = 0.5;

            table.AddColumn("1cm");
            table.AddColumn("7cm");
            table.AddColumn("8cm");

            Row header = table.AddRow();

            header.Shading.Color = Colors.DarkBlue;
            header.Format.Font.Color = Colors.White;
            header.Format.Font.Bold = true;

            header.Cells[0].AddParagraph("#");
            header.Cells[1].AddParagraph("Serial Number");
            header.Cells[2].AddParagraph("Model");

            int index = 1;

            foreach (var item in note.Items)
            {
                Row row = table.AddRow();

                if (index % 2 == 0)
                    row.Shading.Color = Colors.LightYellow;

                row.Cells[0].AddParagraph(index.ToString());
                row.Cells[1].AddParagraph(item.SerialNumber.Value);
                row.Cells[2].AddParagraph(item.SerialNumber.Model.Name);

                index++;
            }
        }

        private void AddFooter(Section section)
        {
            section.AddParagraph();

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
