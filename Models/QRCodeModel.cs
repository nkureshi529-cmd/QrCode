namespace QrCode.Models
{
    public class QRCodeModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }    
        public string Format { get; set; }            // "png", "jpg", "svg"
        public string ForeColor { get; set; }        // e.g. "#000000"
        public string BackColor { get; set; }        // e.g. "#ffffff"
        public string ImageUrl => "/GeneratedQR/" + FileName;   // Auto URL generate

        public bool HasLogo { get; set; }
        public string LogoFileName { get; set; }     // optional
        public DateTime CreatedAt { get; set; }
    }
}
