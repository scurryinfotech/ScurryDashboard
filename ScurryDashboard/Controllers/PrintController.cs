using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("PrintBill")]
        public IActionResult PrintBill([FromBody] PrintRequestModel req)
        {
            if (req == null)
                return BadRequest("Invalid request");

            var printerName = string.IsNullOrWhiteSpace(req.PrinterName)
                ? "Everycom-58-Series"
                : req.PrinterName;

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                string ESC = "\x1B";
                string GS = "\x1D";

                var sb = new StringBuilder();

                // Initialize printer
                sb.Append(ESC + "@");
                sb.Append(ESC + "!" + "\x00");

                // Center align
                sb.Append(ESC + "a" + "\x01");

                sb.AppendLine("GRILL N SHAKES");
                sb.AppendLine("Order Receipt");

                sb.AppendLine("--------------------------------");

                // Left align
                sb.Append(ESC + "a" + "\x00");

                sb.AppendLine($"Order : {req.OrderId}");
                sb.AppendLine($"Time  : {req.OrderTime:dd-MM-yyyy HH:mm}");
                sb.AppendLine($"Order : {req.Name}");
                sb.AppendLine($"Phone : {req.Phone}");

                if (!string.IsNullOrEmpty(req.Address))
                    sb.AppendLine($"Addr  : {req.Address}");

                sb.AppendLine("--------------------------------");

                // Items
                foreach (var it in req.Items ?? Enumerable.Empty<PrintItem>())
                {
                    decimal total = it.Price * it.Quantity;

                    string name = Truncate(it.Name, 16).PadRight(16);
                    string qty = ("x" + it.Quantity).PadLeft(4);
                    string price = total.ToString("0.00").PadLeft(12);

                    sb.AppendLine($"{name}{qty}{price}");
                }

                sb.AppendLine("--------------------------------");

                sb.AppendLine(AlignColumns("Subtotal", $"Rs {req.Subtotal:0.00}", 32));

                if (req.Discount > 0)
                    sb.AppendLine(AlignColumns("Discount", $"-Rs {req.Discount:0.00}", 32));

                sb.Append(ESC + "E" + "\x01"); // Bold
                sb.AppendLine(AlignColumns("TOTAL", $"Rs {req.Total:0.00}", 32));
                sb.Append(ESC + "E" + "\x00");

                sb.AppendLine("--------------------------------");

                sb.Append(ESC + "a" + "\x01");
                sb.AppendLine("Thank You!");
                sb.AppendLine("Visit Again!");

                sb.AppendLine();

                // Cut paper
                sb.Append(GS + "V" + "\x00");

                var bytes = Encoding.GetEncoding(437).GetBytes(sb.ToString());

                bool sent = RawPrinterHelper.SendBytesToPrinter(printerName, bytes);

                if (!sent)
                {
                    int error = Marshal.GetLastWin32Error();
                    return StatusCode(500, $"Printer error {error}");
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

        private static string AlignColumns(string left, string right, int width)
        {
            left ??= "";
            right ??= "";

            int space = width - left.Length - right.Length;
            if (space < 1) space = 1;

            return left + new string(' ', space) + right;
        }
    }
}