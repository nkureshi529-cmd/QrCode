using Microsoft.AspNetCore.Mvc;
using QRCoder;
using QrCode.Data.QuickQR.Data;
using QrCode.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QrCode.Controllers
{
    public class QRCodeController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly IWebHostEnvironment _env;

        // ❌ Remove _context — same as _db
        // private readonly DatabaseHelper _context;

        public QRCodeController(DatabaseHelper db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // =======================
        //      HOME PAGE
        // =======================
        public IActionResult Index()
        {
            var list = _db.GetAllQRCodes();
            return View(list);
        }

        // =======================
        //   CREATE QR CODE
        // =======================
        [HttpPost]
        public async Task<IActionResult> Generate(
            string content,
            string type = "Text",
            string foreColor = "#000000",
            string backColor = "#ffffff",
            string style = "Square",
            IFormFile? logo = null,
            string format = "png")
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Please enter content to generate QR.";
                return RedirectToAction("Index");
            }

            // Ensure folders exist
            var qrcodesPath = Path.Combine(_env.WebRootPath, "images", "qrcodes");
            var logosPath = Path.Combine(_env.WebRootPath, "images", "logos");
            Directory.CreateDirectory(qrcodesPath);
            Directory.CreateDirectory(logosPath);

            // Logo upload
            string logoFileName = null;
            Bitmap logoBitmap = null;
            bool hasLogo = false;

            if (logo != null && logo.Length > 0)
            {
                var ext = Path.GetExtension(logo.FileName);
                logoFileName = $"logo_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
                var logoFull = Path.Combine(logosPath, logoFileName);

                using (var fs = new FileStream(logoFull, FileMode.Create))
                {
                    await logo.CopyToAsync(fs);
                }

                logoBitmap = (Bitmap)Image.FromFile(logoFull);
                hasLogo = true;
            }

            // Create QR
            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            Color fore = ColorTranslator.FromHtml(foreColor ?? "#000000");
            Color back = ColorTranslator.FromHtml(backColor ?? "#ffffff");

            var outFileName = $"qr_{DateTime.Now:yyyyMMddHHmmssfff}.{format}";
            var outFullPath = Path.Combine(qrcodesPath, outFileName);

            if (format.ToLower() == "svg")
            {
                var svgQr = new SvgQRCode(qrData);
                var svgText = svgQr.GetGraphic(10);
                await System.IO.File.WriteAllTextAsync(outFullPath, svgText);
            }
            else
            {
                using var qr = new QRCode(qrData);

                int pixelPerModule = 20;
                Bitmap qrImage;

                if (hasLogo)
                {
                    qrImage = qr.GetGraphic(pixelPerModule, fore, back, logoBitmap, 15);
                }
                else
                {
                    qrImage = qr.GetGraphic(pixelPerModule, fore, back, true);
                }

                if (format == "png")
                    qrImage.Save(outFullPath, ImageFormat.Png);
                else
                {
                    var jpegEncoder = ImageCodecInfo.GetImageEncoders()[1];
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                    qrImage.Save(outFullPath, jpegEncoder, encoderParams);
                }

                qrImage.Dispose();
                logoBitmap?.Dispose();
            }

            // Save in DB
            _db.AddQRCode(content, type, outFileName, format, foreColor, backColor, hasLogo, logoFileName);

            TempData["Success"] = "QR generated successfully.";
            return RedirectToAction("Index");
        }

        // =======================
        // VIEW QR IMAGE
        // =======================
        public IActionResult ViewImage(int id)
        {
            var qr = _db.GetQRCodeById(id);
            if (qr == null) return NotFound();

            var path = Path.Combine(_env.WebRootPath, "images", "qrcodes", qr.FileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            var ext = Path.GetExtension(path).ToLower();
            var mime = ext == ".png" ? "image/png"
                     : ext == ".svg" ? "image/svg+xml"
                     : "image/jpeg";

            return File(System.IO.File.ReadAllBytes(path), mime);
        }

        // =======================
        // DOWNLOAD QR
        // =======================
        public IActionResult Download(int id, string format = "png")
        {
            var qr = _db.GetQRCodeById(id);
            if (qr == null) return NotFound();

            var path = Path.Combine(_env.WebRootPath, "images", "qrcodes", qr.FileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            var ext = format.ToLower() == "svg" ? ".svg"
                    : format.ToLower() == "jpg" || format.ToLower() == "jpeg" ? ".jpg"
                    : ".png";

            if (Path.GetExtension(path).ToLower() == ext)
            {
                var data = System.IO.File.ReadAllBytes(path);
                var mime = ext == ".svg" ? "image/svg+xml"
                        : ext == ".png" ? "image/png"
                        : "image/jpeg";

                return File(data, mime, Path.GetFileName(path));
            }

            using var img = Image.FromFile(path);
            using var ms = new MemoryStream();

            if (ext == ".png")
            {
                img.Save(ms, ImageFormat.Png);
                return File(ms.ToArray(), "image/png", qr.FileName + ".png");
            }
            else
            {
                var encoder = ImageCodecInfo.GetImageEncoders()[1];
                var encParams = new EncoderParameters(1);
                encParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                img.Save(ms, encoder, encParams);
                return File(ms.ToArray(), "image/jpeg", qr.FileName + ".jpg");
            }
        }

        // =======================
        // QR HISTORY PAGE
        // =======================
        public IActionResult History()
        {
            var history = _db.GetAllQRCodes();
            return View(history);
        }
    }
}
