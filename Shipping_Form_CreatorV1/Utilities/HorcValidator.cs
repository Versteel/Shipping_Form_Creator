using System.IO;

namespace Shipping_Form_CreatorV1.Utilities
{
    public static class HocrValidator
    {
        public static void ValidateEnvironment(string ghostscriptPath, string baseDir)
        {
            // 1. Ghostscript check
            if (!File.Exists(ghostscriptPath))
                throw new FileNotFoundException(
                    $"Ghostscript executable not found at: {ghostscriptPath}\n" +
                    "➡ Make sure you installed Ghostscript and set the correct path."
                );

            // 2. tessdata check
            var tessdataDir = Path.Combine(baseDir, "tessdata");
            if (!Directory.Exists(tessdataDir))
                throw new DirectoryNotFoundException(
                    $"tessdata folder not found at: {tessdataDir}\n" +
                    "➡ Copy the tessdata folder from the Utility.Hocr build output."
                );

            var engFile = Path.Combine(tessdataDir, "eng.traineddata");
            if (!File.Exists(engFile))
                throw new FileNotFoundException(
                    $"Missing language file: {engFile}\n" +
                    "➡ You need at least eng.traineddata for English OCR."
                );

            // 3. Native support libraries (x64/x86)
            var x64Dir = Path.Combine(baseDir, "x64");
            var x86Dir = Path.Combine(baseDir, "x86");
            if (!Directory.Exists(x64Dir) || !Directory.Exists(x86Dir))
                throw new DirectoryNotFoundException(
                    $"Missing native support directories (x64/x86) under {baseDir}."
                );

            Console.WriteLine("✅ Hocr environment validated successfully.");
        }
    }
}
