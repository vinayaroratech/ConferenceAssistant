using QRCoder;

namespace ConferenceAssistant.Web;

public static class QrCodeHelper
{
    public static string GenerateSvg(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var svgCode = new SvgQRCode(qrData);
        return svgCode.GetGraphic(5);
    }
}
