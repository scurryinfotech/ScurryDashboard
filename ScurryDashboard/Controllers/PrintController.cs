using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ScurryDashboard.Helpers;
using ScurryDashboard.Models;
using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ScurryDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PrintController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("PrintBill")]
        public IActionResult PrintBill([FromBody] PrintRequestModel req)
        {
            if (req == null)
                return BadRequest("Empty payload");

            var printerName = !string.IsNullOrWhiteSpace(req.PrinterName)
                ? req.PrinterName
                : _configuration["Printer:Name"] ?? "POS-58";

            try
            {
             
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                var sb = new StringBuilder();
                string ESC = "\x1B";
                string GS = "\x1D";

                // Initialize
                sb.Append(ESC + "@");

                // Center + Bold Title
                sb.Append(ESC + "a" + "\x01");
                sb.Append(ESC + "E" + "\x01");
                sb.AppendLine(CenterText("GRILL N SHAKES", 20));
                sb.Append(ESC + "E" + "\x00");

                sb.AppendLine(CenterText("Order Receipt", 20));
                sb.AppendLine(new string('-', 32));

                // Left align
                sb.Append(ESC + "a" + "\x00");
                sb.Append(ESC + "!" + "\x30");
                sb.AppendLine(CenterText($"Order #{req.OrderId}", 32));
                sb.Append(ESC + "!" + "\x00");
                sb.AppendLine($"Time  : {req.OrderTime:dd-MM-yyyy HH:mm}");
                sb.AppendLine($"Phone : {req.Phone}");
                sb.AppendLine(new string('-', 32));

                // Items
                foreach (var it in req.Items ?? Enumerable.Empty<PrintItem>())
                {
                    var total = it.Price * it.Quantity;

                    string name = Truncate(it.Name, 16).PadRight(16);
                    string qty = it.Quantity.ToString().PadLeft(3);
                    string price = total.ToString("0.00").PadLeft(9);

                    sb.AppendLine($"{name}{qty}{price}");
                }

                sb.AppendLine(new string('-', 32));

                sb.AppendLine(AlignColumns("Subtotal", $"Rs {req.Subtotal:0.00}", 20));

                if (req.Discount > 0)
                    sb.AppendLine(AlignColumns("Discount", $"-Rs {req.Discount:0.00}", 20));

                sb.Append(ESC + "E" + "\x01"); 
                sb.AppendLine(AlignColumns("TOTAL", $"Rs {req.Total:0.00}", 20));
                sb.Append(ESC + "E" + "\x00");

                sb.AppendLine(new string('-', 32));

                sb.Append(ESC + "a" + "\x01");
                sb.AppendLine("Thank You!");
                sb.AppendLine("Visit Again!");
                sb.AppendLine();

                // Cut paper
                sb.Append(GS + "V" + "\x00");

                var encoding = Encoding.GetEncoding(437);
                var bytes = encoding.GetBytes(sb.ToString());

                var sent = RawPrinterHelper.SendBytesToPrinter(printerName, bytes);

                if (!sent)
                {
                    int error = Marshal.GetLastWin32Error();
                    return StatusCode(500, $"Printer error. Code: {error}");
                }

                return Ok(new { success = true, printer = printerName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= max ? text : text.Substring(0, max);
        }

        private static string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int padding = (width - text.Length) / 2;
            if (padding < 0) padding = 0;
            return new string(' ', padding) + text;
        }

        private static string AlignColumns(string left, string right, int width)
        {
            left ??= "";
            right ??= "";

            // Truncate left first if too long
            if (left.Length > width - right.Length - 1)
            {
                left = left.Substring(0, width - right.Length - 1);
            }

            int space = width - left.Length - right.Length;
            if (space < 1) space = 1;

            return left + new string(' ', space) + right;
        }
    }
}