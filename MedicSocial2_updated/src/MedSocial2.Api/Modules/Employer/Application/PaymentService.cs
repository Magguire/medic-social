using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Employer.Domain;

namespace Employer.Application;

public record PaymentInitiationResult(bool IsSuccessful, string Status, string? ExternalReference, string? CheckoutReference, string? RedirectUrl, string? RawResponse, string? Error);

public interface IPaymentService
{
    Task<PaymentInitiationResult> InitiateAsync(PaymentProviderConfig config, PaymentTransaction transaction, Dictionary<string, string> payerDetails, CancellationToken cancellationToken);
    Task<PaymentInitiationResult> TestAsync(PaymentProviderConfig config, CancellationToken cancellationToken);
    Task<PaymentInitiationResult> ConfirmAsync(PaymentProviderConfig config, PaymentTransaction transaction, CancellationToken cancellationToken);
}

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    public PaymentService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<PaymentInitiationResult> InitiateAsync(PaymentProviderConfig config, PaymentTransaction transaction, Dictionary<string, string> payerDetails, CancellationToken cancellationToken)
    {
        try
        {
            return config.Provider == PaymentProviderType.Mpesa
                ? await InitiateMpesaAsync(config, transaction, payerDetails, cancellationToken)
                : await InitiatePayPalAsync(config, transaction, cancellationToken);
        }
        catch (Exception ex)
        {
            return new(false, "Failed", null, null, null, null, ex.Message);
        }
    }

    public async Task<PaymentInitiationResult> TestAsync(PaymentProviderConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetAccessTokenAsync(config, cancellationToken);
            return string.IsNullOrWhiteSpace(token)
                ? new(false, "Failed", null, null, null, null, "Provider authentication did not return an access token.")
                : new(true, "Authenticated", null, null, null, null, null);
        }
        catch (Exception ex)
        {
            return new(false, "Failed", null, null, null, null, ex.Message);
        }
    }

    public async Task<PaymentInitiationResult> ConfirmAsync(PaymentProviderConfig config, PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            if (config.Provider != PaymentProviderType.PayPal || string.IsNullOrWhiteSpace(transaction.ExternalReference))
                return new(false, "Failed", null, null, null, null, "This payment cannot be confirmed through the PayPal capture flow.");
            var token = await GetAccessTokenAsync(config, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.ApiBaseUrl.TrimEnd('/')}/v2/checkout/orders/{transaction.ExternalReference}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { });
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) return new(false, "Failed", transaction.ExternalReference, transaction.CheckoutReference, null, raw, raw);
            using var json = JsonDocument.Parse(raw);
            var status = json.RootElement.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
            return string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                ? new(true, "Successful", transaction.ExternalReference, transaction.CheckoutReference, null, raw, null)
                : new(false, status ?? "Failed", transaction.ExternalReference, transaction.CheckoutReference, null, raw, $"PayPal returned status {status}.");
        }
        catch (Exception ex) { return new(false, "Failed", null, null, null, null, ex.Message); }
    }

    private async Task<PaymentInitiationResult> InitiatePayPalAsync(PaymentProviderConfig config, PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(config, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.ApiBaseUrl.TrimEnd('/')}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = transaction.Id.ToString(),
                    custom_id = transaction.Id.ToString(),
                    amount = new { currency_code = transaction.Currency, value = transaction.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) },
                    payee = string.IsNullOrWhiteSpace(config.ReceiverAccount) ? null : new { email_address = config.ReceiverAccount }
                }
            },
            application_context = new { return_url = AppendTransactionId(config.CallbackUrl, transaction.Id), cancel_url = AppendTransactionId(config.CallbackUrl, transaction.Id) }
        });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) return new(false, "Failed", null, null, null, raw, raw);
        using var json = JsonDocument.Parse(raw);
        var id = json.RootElement.GetProperty("id").GetString();
        var redirect = json.RootElement.TryGetProperty("links", out var links)
            ? links.EnumerateArray().FirstOrDefault(item => item.TryGetProperty("rel", out var rel) && rel.GetString() == "approve").GetProperty("href").GetString()
            : null;
        return new(true, "AwaitingCustomerAction", id, id, redirect, raw, null);
    }

    private static string? AppendTransactionId(string? url, Guid transactionId)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        return $"{url}{(url.Contains('?') ? '&' : '?')}transactionId={transactionId}";
    }

    private async Task<PaymentInitiationResult> InitiateMpesaAsync(PaymentProviderConfig config, PaymentTransaction transaction, Dictionary<string, string> payerDetails, CancellationToken cancellationToken)
    {
        if (!payerDetails.TryGetValue("phoneNumber", out var phoneNumber) || string.IsNullOrWhiteSpace(phoneNumber))
            return new(false, "Failed", null, null, null, null, "Phone number is required for M-Pesa.");
        var token = await GetAccessTokenAsync(config, cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.BusinessShortCode}{config.PassKey}{timestamp}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.ApiBaseUrl.TrimEnd('/')}/mpesa/stkpush/v1/processrequest");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            BusinessShortCode = config.BusinessShortCode,
            Password = password,
            Timestamp = timestamp,
            TransactionType = "CustomerPayBillOnline",
            Amount = Math.Ceiling(transaction.Amount),
            PartyA = phoneNumber,
            PartyB = config.BusinessShortCode,
            PhoneNumber = phoneNumber,
            CallBackURL = config.CallbackUrl,
            AccountReference = transaction.Id.ToString("N")[..12],
            TransactionDesc = "MedSocial subscription"
        });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) return new(false, "Failed", null, null, null, raw, raw);
        using var json = JsonDocument.Parse(raw);
        var checkout = json.RootElement.TryGetProperty("CheckoutRequestID", out var checkoutElement) ? checkoutElement.GetString() : null;
        var merchant = json.RootElement.TryGetProperty("MerchantRequestID", out var merchantElement) ? merchantElement.GetString() : null;
        return new(true, "AwaitingCustomerAction", merchant, checkout, null, raw, null);
    }

    private async Task<string> GetAccessTokenAsync(PaymentProviderConfig config, CancellationToken cancellationToken)
    {
        var endpoint = config.Provider == PaymentProviderType.Mpesa
            ? $"{config.ApiBaseUrl.TrimEnd('/')}/oauth/v1/generate?grant_type=client_credentials"
            : $"{config.ApiBaseUrl.TrimEnd('/')}/v1/oauth2/token";
        using var request = new HttpRequestMessage(config.Provider == PaymentProviderType.Mpesa ? HttpMethod.Get : HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));
        if (config.Provider == PaymentProviderType.PayPal)
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(raw);
        return json.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() ?? string.Empty : string.Empty;
    }
}
