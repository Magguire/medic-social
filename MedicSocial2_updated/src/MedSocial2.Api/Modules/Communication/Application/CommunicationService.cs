using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Communication.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Communication.Application
{
    public record SendCommunicationRequest(
        CommunicationChannel Channel,
        string Recipient,
        string Subject,
        string Body,
        Guid? TenantId,
        Guid? UserId,
        string? TemplateKey,
        string? RelatedEntityName,
        string? RelatedEntityId);

    public record CommunicationProviderConfigDto(
        Guid Id,
        CommunicationChannel Channel,
        string ProviderName,
        bool IsEnabled,
        string? BaseUrl,
        string? SenderId,
        string? AccountSid,
        string? TemplateNamespace,
        bool SimulateWhenDisabled,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public record UpsertCommunicationProviderConfigDto(
        CommunicationChannel Channel,
        string ProviderName,
        bool IsEnabled,
        string? BaseUrl,
        string? SenderId,
        string? ApiKeySecret,
        string? AccountSid,
        string? TemplateNamespace,
        bool SimulateWhenDisabled);

    public record CommunicationMessageDto(
        Guid Id,
        Guid? TenantId,
        Guid? UserId,
        CommunicationChannel Channel,
        string Recipient,
        string Subject,
        string Body,
        string? TemplateKey,
        string? RelatedEntityName,
        string? RelatedEntityId,
        string ProviderName,
        CommunicationMessageStatus Status,
        string? ProviderResponse,
        DateTime CreatedAt,
        DateTime? SentAt);

    public interface ICommunicationService
    {
        Task<CommunicationMessageDto> SendAsync(SendCommunicationRequest request, CancellationToken cancellationToken = default);
    }

    public class CommunicationService : ICommunicationService
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _httpClient;

        public CommunicationService(ApplicationDbContext db, HttpClient httpClient)
        {
            _db = db;
            _httpClient = httpClient;
        }

        public async Task<CommunicationMessageDto> SendAsync(SendCommunicationRequest request, CancellationToken cancellationToken = default)
        {
            var config = await _db.CommunicationProviderConfigs
                .FirstOrDefaultAsync(c => c.Channel == request.Channel, cancellationToken);

            var providerName = config?.ProviderName ?? "Not configured";
            var canSend = config?.IsEnabled == true && !string.IsNullOrWhiteSpace(config.ApiKeySecret);
            CommunicationMessageStatus status;
            string providerResponse;

            if (canSend && config is not null)
            {
                try
                {
                    providerResponse = await DispatchAsync(config, request, cancellationToken);
                    status = CommunicationMessageStatus.Sent;
                }
                catch (Exception ex)
                {
                    providerResponse = ex.Message;
                    status = CommunicationMessageStatus.Failed;
                }
            }
            else
            {
                providerResponse = "Provider is not fully configured; message was recorded as simulated.";
                status = CommunicationMessageStatus.Simulated;
            }

            var message = new CommunicationMessage
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = request.UserId,
                Channel = request.Channel,
                Recipient = request.Recipient,
                Subject = request.Subject,
                Body = request.Body,
                TemplateKey = request.TemplateKey,
                RelatedEntityName = request.RelatedEntityName,
                RelatedEntityId = request.RelatedEntityId,
                ProviderName = providerName,
                Status = status,
                ProviderResponse = providerResponse,
                SentAt = status is CommunicationMessageStatus.Sent or CommunicationMessageStatus.Simulated ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            _db.CommunicationMessages.Add(message);
            await _db.SaveChangesAsync(cancellationToken);
            return Map(message);
        }

        private async Task<string> DispatchAsync(CommunicationProviderConfig config, SendCommunicationRequest request, CancellationToken cancellationToken)
        {
            return config.Channel switch
            {
                CommunicationChannel.Email => await SendEmailAsync(config, request, cancellationToken),
                CommunicationChannel.Sms => await SendHttpMessageAsync(config, request, "sms", cancellationToken),
                CommunicationChannel.WhatsApp => await SendHttpMessageAsync(config, request, "whatsapp", cancellationToken),
                _ => throw new NotSupportedException($"Unsupported communication channel {config.Channel}")
            };
        }

        private static async Task<string> SendEmailAsync(CommunicationProviderConfig config, SendCommunicationRequest request, CancellationToken cancellationToken)
        {
            var (host, port, enableSsl) = ParseSmtpEndpoint(config.BaseUrl);
            using var message = new MailMessage(config.SenderId ?? "no-reply@medicsocial.local", request.Recipient)
            {
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = request.Body.Contains('<')
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(config.AccountSid) || !string.IsNullOrWhiteSpace(config.ApiKeySecret))
            {
                client.Credentials = new NetworkCredential(config.AccountSid ?? config.SenderId, config.ApiKeySecret);
            }

            await client.SendMailAsync(message, cancellationToken);
            return $"SMTP accepted by {host}:{port}.";
        }

        private async Task<string> SendHttpMessageAsync(CommunicationProviderConfig config, SendCommunicationRequest request, string channel, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                throw new InvalidOperationException($"{channel} provider base URL is required.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl);
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKeySecret);
            httpRequest.Content = JsonContent.Create(new
            {
                channel,
                to = request.Recipient,
                from = config.SenderId,
                subject = request.Subject,
                body = request.Body,
                template = request.TemplateKey,
                accountSid = config.AccountSid,
                templateNamespace = config.TemplateNamespace
            });

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"{channel} provider returned {(int)response.StatusCode}: {body}");
            }

            return string.IsNullOrWhiteSpace(body) ? $"{channel} provider accepted the message." : body;
        }

        private static (string Host, int Port, bool EnableSsl) ParseSmtpEndpoint(string? endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return ("localhost", 25, false);
            }

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                var port = uri.IsDefaultPort ? (uri.Scheme.Equals("smtps", StringComparison.OrdinalIgnoreCase) ? 465 : 25) : uri.Port;
                return (uri.Host, port, uri.Scheme.Equals("smtps", StringComparison.OrdinalIgnoreCase));
            }

            var parts = endpoint.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && int.TryParse(parts[1], out var parsedPort))
            {
                return (parts[0], parsedPort, parsedPort is 465 or 587);
            }

            return (endpoint, 25, false);
        }

        public static CommunicationProviderConfigDto Map(CommunicationProviderConfig config) => new(
            config.Id,
            config.Channel,
            config.ProviderName,
            config.IsEnabled,
            config.BaseUrl,
            config.SenderId,
            config.AccountSid,
            config.TemplateNamespace,
            config.SimulateWhenDisabled,
            config.CreatedAt,
            config.UpdatedAt);

        public static CommunicationMessageDto Map(CommunicationMessage message) => new(
            message.Id,
            message.TenantId,
            message.UserId,
            message.Channel,
            message.Recipient,
            message.Subject,
            message.Body,
            message.TemplateKey,
            message.RelatedEntityName,
            message.RelatedEntityId,
            message.ProviderName,
            message.Status,
            message.ProviderResponse,
            message.CreatedAt,
            message.SentAt);
    }
}
