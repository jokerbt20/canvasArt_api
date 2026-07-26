using System.Globalization;
using System.Net;
using System.Text;
using CanvasArt.API.Models.DTOs.Orders;

namespace CanvasArt.API.Services;

/// <summary>
/// Builds the HTML bodies for the two emails sent when an order is placed: a confirmation for the
/// customer and a full summary for the shop inbox. Kept separate from <see cref="OrderService"/>
/// so the markup is easy to tweak without touching order logic.
/// </summary>
public static class OrderEmailComposer
{
    /// <summary>Confirmation sent to the customer's email address.</summary>
    public static string BuildCustomerEmail(OrderDetailDto order)
    {
        var intro = $"""
            <p>Здраво {Enc(order.FirstName)},</p>
            <p>Ви благодариме за вашата нарачка во <strong>CanvasArts</strong>! Ја примивме и наскоро ќе ве контактираме.
            Подолу е прегледот на вашата нарачка.</p>
            <p>Hello {Enc(order.FirstName)}, thank you for your order at <strong>CanvasArts</strong>.
            We've received it and will contact you shortly. Your order summary is below.</p>
            """;
        return Wrap($"Нарачка / Order {Enc(order.OrderNumber)}", intro, order, includeCustomerBlock: false, includeDistributor: false);
    }

    /// <summary>Full summary sent to the shop inbox (info@canvasarts.mk).</summary>
    public static string BuildBusinessEmail(OrderDetailDto order)
    {
        var intro = $"""
            <p>Нова нарачка е примена: <strong>{Enc(order.OrderNumber)}</strong>.</p>
            """;
        return Wrap($"Нова нарачка / New order {Enc(order.OrderNumber)}", intro, order, includeCustomerBlock: true, includeDistributor: true);
    }

    private static string Wrap(string heading, string intro, OrderDetailDto order, bool includeCustomerBlock, bool includeDistributor)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <div style="font-family:Arial,Helvetica,sans-serif;color:#222;max-width:640px;margin:0 auto;">
            """);
        sb.Append($"<h2 style=\"color:#111;\">{heading}</h2>");
        sb.Append($"<p style=\"color:#666;margin:0 0 16px;\">{order.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}</p>");
        sb.Append(intro);

        if (includeCustomerBlock)
            sb.Append(BuildCustomerBlock(order));

        sb.Append(BuildItemsTable(order));
        sb.Append(BuildTotals(order, includeDistributor));

        sb.Append("""
            <p style="color:#888;font-size:12px;margin-top:24px;">CanvasArts · canvasarts.mk</p>
            </div>
            """);
        return sb.ToString();
    }

    private static string BuildCustomerBlock(OrderDetailDto o)
    {
        var address = $"{Enc(o.AddressLine)}, {Enc(o.PostalCode)} {Enc(o.City)}, {Enc(o.Country)}";
        var notes = string.IsNullOrWhiteSpace(o.Notes) ? "" : $"<p><strong>Забелешки / Notes:</strong> {Enc(o.Notes!)}</p>";
        return $"""
            <div style="background:#f7f7f7;padding:12px 16px;border-radius:6px;margin:16px 0;">
              <p style="margin:0 0 4px;"><strong>{Enc(o.FirstName)} {Enc(o.LastName)}</strong></p>
              <p style="margin:0 0 4px;">{Enc(o.Email)} · {Enc(o.Phone)}</p>
              <p style="margin:0;">{address}</p>
              {notes}
            </div>
            """;
    }

    private static string BuildItemsTable(OrderDetailDto o)
    {
        var rows = new StringBuilder();
        foreach (var item in o.Items)
        {
            var frame = string.IsNullOrWhiteSpace(item.FrameName) ? "" : $" · {Enc(item.FrameName!)}";
            rows.Append($"""
                <tr>
                  <td style="padding:8px;border-bottom:1px solid #eee;">
                    {Enc(item.PaintingName)} <span style="color:#888;">({Enc(item.PaintingCode)})</span><br/>
                    <span style="color:#666;font-size:13px;">{Enc(item.SizeLabel)}{frame}</span>
                  </td>
                  <td style="padding:8px;border-bottom:1px solid #eee;text-align:center;">{item.Quantity}</td>
                  <td style="padding:8px;border-bottom:1px solid #eee;text-align:right;">{Money(item.LineTotal)}</td>
                </tr>
                """);
        }

        return $"""
            <table style="width:100%;border-collapse:collapse;margin:16px 0;">
              <thead>
                <tr>
                  <th style="padding:8px;text-align:left;border-bottom:2px solid #ddd;">Артикл / Item</th>
                  <th style="padding:8px;text-align:center;border-bottom:2px solid #ddd;">Кол. / Qty</th>
                  <th style="padding:8px;text-align:right;border-bottom:2px solid #ddd;">Износ / Total</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
            """;
    }

    private static string BuildTotals(OrderDetailDto o, bool includeDistributor)
    {
        var sb = new StringBuilder();
        sb.Append("<table style=\"width:100%;max-width:320px;margin-left:auto;border-collapse:collapse;\">");
        sb.Append(TotalRow("Меѓузбир / Subtotal", Money(o.SubTotal)));
        if (o.DiscountTotal > 0)
            sb.Append(TotalRow("Попуст / Discount", "−" + Money(o.DiscountTotal)));
        if (o.PromoDiscount > 0)
        {
            var label = string.IsNullOrWhiteSpace(o.PromoCode)
                ? "Промо попуст / Promo discount"
                : $"Промо / Promo ({Enc(o.PromoCode!)})";
            sb.Append(TotalRow(label, "−" + Money(o.PromoDiscount)));
        }
        sb.Append(TotalRow("Достава / Shipping", Money(o.ShippingCost)));
        sb.Append(TotalRow("<strong>Вкупно / Total</strong>", $"<strong>{Money(o.GrandTotal)}</strong>", top: true));
        sb.Append("</table>");

        if (includeDistributor && !string.IsNullOrWhiteSpace(o.DistributorName))
        {
            sb.Append($"""
                <p style="margin-top:16px;color:#444;">
                  <strong>Дистрибутер / Distributor:</strong> {Enc(o.DistributorName!)}
                  {(string.IsNullOrWhiteSpace(o.PromoCode) ? "" : $" · {Enc(o.PromoCode!)}")}
                </p>
                """);
        }

        return sb.ToString();
    }

    private static string TotalRow(string label, string value, bool top = false)
    {
        var border = top ? "border-top:2px solid #ddd;" : "";
        return $"""
            <tr>
              <td style="padding:6px 8px;{border}">{label}</td>
              <td style="padding:6px 8px;text-align:right;{border}">{value}</td>
            </tr>
            """;
    }

    private static string Money(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture) + " ден";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
