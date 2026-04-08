using Convert_Word_Document_to_PDF.Models;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using System.Diagnostics;
using System.IO;

namespace Convert_Word_Document_to_PDF.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public ActionResult SkiaSharpOnly()
        {
            using var bitmap = new SKBitmap(500, 500);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            string text = "Hello SkaiSharp";
            using var typeface = SKTypeface.FromFamilyName("Arial");
            using var paint = new SKPaint
            {
                Color = SKColors.Blue,
                TextSize = 40,
                IsAntialias = true,
                Typeface = typeface
            };
            float textWidth = paint.MeasureText(text);
            var fm = paint.FontMetrics;
            float x = (bitmap.Width - textWidth) / 2;
            float y = (bitmap.Height / 2) - ((fm.Ascent + fm.Descent) / 2);
            canvas.DrawText(text, x, y, paint);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return File(data.ToArray(), "image/png", "SkiaSharp_Test.png");
        }

        public ActionResult ConvertWordtoPDF()
        {
            //Open the file as Stream
            using (FileStream docStream = new FileStream(Path.GetFullPath("Data/Japanese.docx"), FileMode.Open, FileAccess.Read))
            {
                //Loads file stream into Word document
                using (WordDocument wordDocument = new WordDocument(docStream, FormatType.Automatic))
                {
                    //Hooks the font substitution event
                    wordDocument.FontSettings.SubstituteFont += FontSettings_SubstituteFont;
                    //Instantiation of DocIORenderer for Word to PDF conversion
                    using (DocIORenderer render = new DocIORenderer())
                    {
                        //Converts Word document into PDF document
                        PdfDocument pdfDocument = render.ConvertToPDF(wordDocument);

                        //Saves the PDF document to MemoryStream.
                        MemoryStream stream = new MemoryStream();
                        pdfDocument.Save(stream);
                        stream.Position = 0;

                        //Download PDF document in the browser.
                        return File(stream, "application/pdf", "Sample.pdf");
                    }
                }
            }
        }
        private void FontSettings_SubstituteFont(object sender, SubstituteFontEventArgs args)
        {
            //Sets the alternate font when a specified font is not installed in the production environment
            //Sets the alternate font based on the font style.
            switch (args.OrignalFontName)
            {
                case "Arial":
                    args.AlternateFontStream = new FileStream(Path.GetFullPath("Fonts/arial.ttf"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    break;
                case "MS Gothic":
                    args.AlternateFontStream = new FileStream(Path.GetFullPath("Fonts/msgothic.ttc"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    break;
                case "Aptos":
                    args.AlternateFontStream = new FileStream(Path.GetFullPath("Fonts/Aptos.ttf"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    break;
                case "Leelawadee UI":
                    args.AlternateFontStream = new FileStream(Path.GetFullPath("Fonts/LeelawUI.ttf"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    break;
                default:
                    args.AlternateFontStream = new FileStream(Path.GetFullPath("Fonts/times.ttf"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    break;
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
