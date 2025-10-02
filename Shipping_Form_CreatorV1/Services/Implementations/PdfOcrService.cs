using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharp.Xps;
using Utility.Hocr.Enums;
using Utility.Hocr.Pdf;

namespace Shipping_Form_CreatorV1.Services.Implementations
{
    public class PdfOcrService
    {
        private readonly string _ghostscriptPath;

        public PdfOcrService(string ghostscriptPath)
        {
            _ghostscriptPath = ghostscriptPath;
        }

        public void ConvertXpsToSearchablePdf(byte[] xpsBytes, string outputFilePath)
        {
            // Step 1: Save XPS to a temp file
            string tempXpsPath = Path.ChangeExtension(Path.GetTempFileName(), ".xps");
            File.WriteAllBytes(tempXpsPath, xpsBytes);

            // Step 2: Convert XPS → normal PDF (un-OCR’d)
            string tempPdfPath = Path.ChangeExtension(Path.GetTempFileName(), ".pdf");
            XpsConverter.Convert(tempXpsPath, tempPdfPath, 0);

            // Step 3: Run HOCR to make it searchable
            var settings = new PdfCompressorSettings
            {
                PdfCompatibilityLevel = PdfCompatibilityLevel.Acrobat_7_1_6,
                WriteTextMode = WriteTextMode.Word,
                Dpi = 400,
                ImageType = PdfImageType.Jpg,
                ImageQuality = 100,
                CompressFinalPdf = true,
                DistillerMode = dPdfSettings.prepress
            };

            using (var comp = new PdfCompressor(_ghostscriptPath, settings))
            {
                byte[] pdfBytes = File.ReadAllBytes(tempPdfPath);
                var result = comp.CreateSearchablePdf(pdfBytes, new PdfMeta());
                File.WriteAllBytes(outputFilePath, result.Item1);
            }

            // Clean up temp files
            if (File.Exists(tempXpsPath)) File.Delete(tempXpsPath);
            if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
        }
    }
}
