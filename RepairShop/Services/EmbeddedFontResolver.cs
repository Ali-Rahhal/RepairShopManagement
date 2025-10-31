using PdfSharp.Fonts;
using System.Reflection;

namespace RepairShop.Services
{
    public class EmbeddedFontResolver : IFontResolver
    {
        private static readonly string ResourcePrefix = "RepairShop.wwwroot.fonts."; // 👈 replace with your namespace
        private static readonly string FontName = "OpenSans";

        public string DefaultFontName => FontName;

        public byte[] GetFont(string faceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("OpenSans-Regular.ttf", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(resourceName)) throw new InvalidOperationException("Font not found in resources.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException("Font not found in resources.");
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(FontName);
        }
    }
}
