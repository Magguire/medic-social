using System.Security.Claims;
using System.Text.Json;
using Employer.Application;
using Employer.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;

namespace Employer.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISubscriptionService _subscriptions;
    private readonly IPaymentService _payments;

    public SubscriptionsController(ApplicationDbContext db, ISubscriptionService subscriptions, IPaymentService payments)
    {
        _db = db;
        _subscriptions = subscriptions;
        _payments = payments;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Plans()
    {
        try
        {
            return Ok(await _db.SubscriptionPlans.OrderBy(plan => plan.PriceAmount).ToListAsync());
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "Employer,Recruiter,SuperAdmin,TenantAdmin")]
    [HttpGet("employer/{employerId:guid}")]
    public async Task<IActionResult> Current(Guid employerId)
    {
        try
        {
            var context = await _subscriptions.GetCurrentAsync(employerId, HttpContext.RequestAborted);
            if (context == null) return NotFound(new { errors = new[] { "Employer subscription was not found." } });
            var usages = await _db.SubscriptionUsages.Where(item => item.EmployerId == employerId && item.PeriodEndsAt > DateTime.UtcNow).ToListAsync();
            var paymentRows = await _db.PaymentTransactions.Where(item => item.EmployerId == employerId).OrderByDescending(item => item.CreatedAt).Take(20).ToListAsync();
            var payments = paymentRows.Select(item => new { item.Id, item.Amount, item.Currency, provider = item.Provider?.ToString(), status = item.Status.ToString(), item.ExternalReference, item.CheckoutReference, item.FailureReason, item.CreatedAt, item.CompletedAt });
            var subscription = context.Subscription == null ? null : new { context.Subscription.Id, status = context.Subscription.Status.ToString(), context.Subscription.StartsAt, context.Subscription.EndsAt, context.Subscription.AutoRenew, context.Subscription.ProvisioningSource, context.Subscription.Notes };
            return Ok(new { context.Employer, context.Plan, subscription, context.IsLegacyFallback, usages, payments });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "Employer,Recruiter")]
    [HttpGet("payment-methods")]
    public async Task<IActionResult> PaymentMethods()
    {
        try
        {
            var items = await _db.PaymentProviderConfigs.Where(item => item.IsEnabled).Select(item => new
            {
                item.Provider, item.DisplayName, item.Currency, item.PromptFieldsJson, item.ReceiverAccount
            }).ToListAsync();
            return Ok(items);
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize]
    [HttpGet("paygo/status")]
    public async Task<IActionResult> PayAsYouGoStatus([FromQuery] PayAsYouGoAction action, [FromQuery] Guid? employerId = null)
    {
        try
        {
            var userId = CurrentUserId();
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null) return Unauthorized(new { errors = new[] { "User session was not found." } });
            var rule = await _db.PayAsYouGoRules.FirstOrDefaultAsync(item => item.Action == action && item.IsEnabled);
            if (rule == null) return Ok(new { isEnabled = false, isChargeRequired = false });
            var period = PeriodKey(rule.PeriodKey);
            var used = await _db.PayAsYouGoCharges.CountAsync(item =>
                item.Action == action &&
                item.PeriodKey == period &&
                item.Status != PayAsYouGoChargeStatus.Failed &&
                (employerId.HasValue ? item.EmployerId == employerId.Value : item.UserId == userId));
            var isChargeRequired = used >= rule.FreeUnitsPerPeriod;
            return Ok(new
            {
                isEnabled = true,
                isChargeRequired,
                used,
                freeUnits = rule.FreeUnitsPerPeriod,
                remainingFreeUnits = Math.Max(0, rule.FreeUnitsPerPeriod - used),
                rule.UnitPrice,
                rule.Currency,
                rule.RequirePaymentBeforeAction,
                rule.Description
            });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize]
    [HttpPost("paygo/record")]
    public async Task<IActionResult> RecordPayAsYouGo([FromBody] PayAsYouGoRecordRequest request)
    {
        try
        {
            var userId = CurrentUserId();
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null) return Unauthorized(new { errors = new[] { "User session was not found." } });
            var rule = await _db.PayAsYouGoRules.FirstOrDefaultAsync(item => item.Action == request.Action && item.IsEnabled);
            if (rule == null) return Ok(new { status = "NotConfigured", isChargeRequired = false });

            var period = PeriodKey(rule.PeriodKey);
            var used = await _db.PayAsYouGoCharges.CountAsync(item =>
                item.Action == request.Action &&
                item.PeriodKey == period &&
                item.Status != PayAsYouGoChargeStatus.Failed &&
                (request.EmployerId.HasValue ? item.EmployerId == request.EmployerId.Value : item.UserId == userId));
            var isChargeRequired = used >= rule.FreeUnitsPerPeriod;
            var provider = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.IsEnabled);
            var status = !isChargeRequired
                ? PayAsYouGoChargeStatus.Free
                : provider == null
                    ? PayAsYouGoChargeStatus.PendingAdminReview
                    : PayAsYouGoChargeStatus.PendingPayment;

            var charge = new PayAsYouGoCharge
            {
                Id = Guid.NewGuid(),
                Action = request.Action,
                UserId = userId,
                EmployerId = request.EmployerId,
                TenantId = user.TenantId,
                RelatedEntityId = request.RelatedEntityId,
                UnitPrice = isChargeRequired ? rule.UnitPrice : 0,
                Amount = isChargeRequired ? rule.UnitPrice : 0,
                Currency = rule.Currency,
                Status = rule.UnitPrice <= 0 ? PayAsYouGoChargeStatus.Free : status,
                PeriodKey = period,
                PayerDetailsJson = JsonSerializer.Serialize(request.PayerDetails ?? new Dictionary<string, string>()),
                CreatedAt = DateTime.UtcNow,
                CompletedAt = !isChargeRequired || rule.UnitPrice <= 0 ? DateTime.UtcNow : null,
                FailureReason = isChargeRequired && provider == null ? "No automated payment provider is enabled. Awaiting administrator review." : null
            };
            _db.PayAsYouGoCharges.Add(charge);
            await _db.SaveChangesAsync();

            if (isChargeRequired && rule.RequirePaymentBeforeAction && charge.Status is PayAsYouGoChargeStatus.PendingPayment or PayAsYouGoChargeStatus.PendingAdminReview)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    isChargeRequired = true,
                    chargeId = charge.Id,
                    charge.Status,
                    rule.UnitPrice,
                    rule.Currency,
                    provider = provider == null ? null : new { provider.Provider, provider.DisplayName, provider.PromptFieldsJson, provider.ReceiverAccount },
                    message = provider == null ? "Payment request received for administrator review." : "Payment is required to continue."
                });
            }

            return Ok(new { isChargeRequired, chargeId = charge.Id, status = charge.Status.ToString(), amount = charge.Amount, charge.Currency });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "Employer,Recruiter")]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] SubscriptionCheckoutRequest request)
    {
        try
        {
            var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == request.PlanId);
            var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(item => item.Id == request.EmployerId);
            if (plan == null || employer == null) return BadRequest(new { errors = new[] { "Employer or subscription plan was not found." } });

            var providerConfig = request.Provider.HasValue
                ? await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.Provider == request.Provider.Value && item.IsEnabled)
                : await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.IsEnabled);
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(), EmployerId = employer.Id, TenantId = employer.TenantId, PlanId = plan.Id,
                Provider = providerConfig?.Provider, Amount = plan.PriceAmount, Currency = plan.Currency,
                PayerDetailsJson = JsonSerializer.Serialize(request.PayerDetails ?? new Dictionary<string, string>()),
                Status = providerConfig == null ? PaymentTransactionStatus.PendingAdminReview : PaymentTransactionStatus.Pending
            };
            _db.PaymentTransactions.Add(transaction);

            if (plan.PriceAmount <= 0)
            {
                transaction.Status = PaymentTransactionStatus.Successful;
                transaction.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                var subscription = await _subscriptions.ActivateAsync(employer.Id, plan.Id, "FreePlan", transaction.Id, null, null, null, HttpContext.RequestAborted);
                transaction.EmployerSubscriptionId = subscription.Id;
                await _db.SaveChangesAsync();
                return Ok(new { status = transaction.Status.ToString(), message = "Subscription activated.", transactionId = transaction.Id });
            }

            if (providerConfig == null)
            {
                await _db.SaveChangesAsync();
                return Accepted(new { status = transaction.Status.ToString(), message = "Your upgrade request has been received for administrator review.", transactionId = transaction.Id });
            }

            var initiation = await _payments.InitiateAsync(providerConfig, transaction, request.PayerDetails ?? new(), HttpContext.RequestAborted);
            transaction.ExternalReference = initiation.ExternalReference;
            transaction.CheckoutReference = initiation.CheckoutReference;
            transaction.ProviderResponseJson = initiation.RawResponse;
            transaction.FailureReason = initiation.Error;
            transaction.Status = initiation.IsSuccessful ? PaymentTransactionStatus.AwaitingCustomerAction : PaymentTransactionStatus.Failed;
            await _db.SaveChangesAsync();
            return initiation.IsSuccessful
                ? Ok(new { status = transaction.Status.ToString(), transactionId = transaction.Id, initiation.RedirectUrl, message = initiation.RedirectUrl == null ? "Payment request initiated. Complete it on your device." : "Continue to the payment provider." })
                : BadRequest(new { errors = new[] { initiation.Error ?? "Payment initiation failed." }, transactionId = transaction.Id });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "Employer,Recruiter")]
    [HttpPost("payments/{transactionId:guid}/confirm")]
    public async Task<IActionResult> ConfirmPayment(Guid transactionId)
    {
        try
        {
            var transaction = await _db.PaymentTransactions.FirstOrDefaultAsync(item => item.Id == transactionId);
            if (transaction == null || transaction.Provider == null) return NotFound(new { errors = new[] { "Payment transaction was not found." } });
            var config = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.Provider == transaction.Provider && item.IsEnabled);
            if (config == null) return BadRequest(new { errors = new[] { "Payment provider is not available." } });
            var result = await _payments.ConfirmAsync(config, transaction, HttpContext.RequestAborted);
            transaction.ProviderResponseJson = result.RawResponse;
            transaction.FailureReason = result.Error;
            if (!result.IsSuccessful)
            {
                transaction.Status = PaymentTransactionStatus.Failed;
                await _db.SaveChangesAsync();
                return BadRequest(new { errors = new[] { result.Error ?? "Payment confirmation failed." } });
            }
            transaction.Status = PaymentTransactionStatus.Successful;
            transaction.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            var subscription = await _subscriptions.ActivateAsync(transaction.EmployerId, transaction.PlanId, transaction.Provider.ToString()!, transaction.Id, null, null, null, HttpContext.RequestAborted);
            transaction.EmployerSubscriptionId = subscription.Id;
            await _db.SaveChangesAsync();
            return Ok(new { status = "Successful", message = "Payment confirmed and subscription activated." });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpGet("admin/payment-configs")]
    public async Task<IActionResult> Configs()
    {
        try
        {
            var rows = await _db.PaymentProviderConfigs.OrderBy(item => item.Provider).ToListAsync();
            return Ok(rows.Select(item => new
            {
                item.Id, item.Provider, item.DisplayName, item.IsEnabled, item.IsSandbox, item.ApiBaseUrl, item.ClientId,
                hasClientSecret = !string.IsNullOrWhiteSpace(item.ClientSecret), item.BusinessShortCode,
                hasPassKey = !string.IsNullOrWhiteSpace(item.PassKey), item.ReceiverAccount, item.CallbackUrl,
                hasCallbackVerificationToken = !string.IsNullOrWhiteSpace(item.CallbackVerificationToken),
                item.Currency, item.PromptFieldsJson, item.CreatedAt, item.UpdatedAt
            }));
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPost("admin/payment-configs")]
    public async Task<IActionResult> SaveConfig([FromBody] PaymentProviderConfigRequest request)
    {
        try
        {
            var item = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(config => config.Provider == request.Provider);
            if (item == null) { item = new PaymentProviderConfig { Id = Guid.NewGuid(), Provider = request.Provider }; _db.PaymentProviderConfigs.Add(item); }
            item.DisplayName = request.DisplayName; item.IsEnabled = request.IsEnabled; item.IsSandbox = request.IsSandbox;
            item.ApiBaseUrl = request.ApiBaseUrl; item.ClientId = request.ClientId;
            if (!string.IsNullOrWhiteSpace(request.ClientSecret)) item.ClientSecret = request.ClientSecret;
            item.BusinessShortCode = request.BusinessShortCode;
            if (!string.IsNullOrWhiteSpace(request.PassKey)) item.PassKey = request.PassKey;
            item.ReceiverAccount = request.ReceiverAccount; item.CallbackUrl = request.CallbackUrl;
            if (!string.IsNullOrWhiteSpace(request.CallbackVerificationToken)) item.CallbackVerificationToken = request.CallbackVerificationToken;
            item.Currency = request.Currency; item.PromptFieldsJson = request.PromptFieldsJson; item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(item);
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [AllowAnonymous]
    [HttpPost("payments/callback/{provider}")]
    public async Task<IActionResult> PaymentCallback(PaymentProviderType provider, [FromQuery] string token, [FromBody] JsonElement payload)
    {
        try
        {
            var config = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.Provider == provider && item.IsEnabled);
            if (config == null || string.IsNullOrWhiteSpace(config.CallbackVerificationToken) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(config.CallbackVerificationToken),
                    System.Text.Encoding.UTF8.GetBytes(token ?? string.Empty)))
                return Unauthorized(new { errors = new[] { "Invalid payment callback token." } });

            string? checkoutReference = null;
            var successful = false;
            if (provider == PaymentProviderType.Mpesa && payload.TryGetProperty("Body", out var body) && body.TryGetProperty("stkCallback", out var callback))
            {
                checkoutReference = callback.TryGetProperty("CheckoutRequestID", out var checkout) ? checkout.GetString() : null;
                successful = callback.TryGetProperty("ResultCode", out var resultCode) && resultCode.GetInt32() == 0;
            }
            else
            {
                checkoutReference = payload.TryGetProperty("resource", out var resource) &&
                    resource.TryGetProperty("supplementary_data", out var supplementary) &&
                    supplementary.TryGetProperty("related_ids", out var relatedIds) &&
                    relatedIds.TryGetProperty("order_id", out var orderId)
                        ? orderId.GetString()
                        : resource.ValueKind != JsonValueKind.Undefined && resource.TryGetProperty("id", out var resourceId)
                            ? resourceId.GetString()
                            : payload.TryGetProperty("id", out var id) ? id.GetString() : null;
                successful = payload.TryGetProperty("event_type", out var eventType) &&
                    (eventType.GetString()?.Contains("COMPLETED", StringComparison.OrdinalIgnoreCase) == true ||
                     eventType.GetString()?.Contains("CAPTURE", StringComparison.OrdinalIgnoreCase) == true);
            }

            var transaction = await _db.PaymentTransactions.FirstOrDefaultAsync(item =>
                item.Provider == provider && (item.CheckoutReference == checkoutReference || item.ExternalReference == checkoutReference));
            if (transaction == null) return NotFound(new { errors = new[] { "Payment transaction was not found." } });
            transaction.ProviderResponseJson = payload.GetRawText();
            if (!successful)
            {
                transaction.Status = PaymentTransactionStatus.Failed;
                transaction.FailureReason = "Provider reported an unsuccessful payment.";
                await _db.SaveChangesAsync();
                return Ok(new { received = true });
            }

            transaction.Status = PaymentTransactionStatus.Successful;
            transaction.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            var subscription = await _subscriptions.ActivateAsync(transaction.EmployerId, transaction.PlanId, provider.ToString(), transaction.Id, null, null, null, HttpContext.RequestAborted);
            transaction.EmployerSubscriptionId = subscription.Id;
            await _db.SaveChangesAsync();
            return Ok(new { received = true, activated = true });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPost("admin/payment-configs/{provider}/test")]
    public async Task<IActionResult> Test(PaymentProviderType provider)
    {
        try
        {
            var config = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.Provider == provider);
            if (config == null) return NotFound(new { errors = new[] { "Payment provider is not configured." } });
            var result = await _payments.TestAsync(config, HttpContext.RequestAborted);
            return result.IsSuccessful ? Ok(result) : BadRequest(new { errors = new[] { result.Error } });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpGet("admin/transactions")]
    public async Task<IActionResult> Transactions()
    {
        try
        {
            var rows = await _db.PaymentTransactions.OrderByDescending(item => item.CreatedAt).Take(250).ToListAsync();
            return Ok(rows.Select(item => new { item.Id, item.EmployerId, item.PlanId, provider = item.Provider?.ToString(), status = item.Status.ToString(), item.Amount, item.Currency, item.ExternalReference, item.CheckoutReference, item.FailureReason, item.CreatedAt, item.CompletedAt }));
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpGet("admin/paygo-rules")]
    public async Task<IActionResult> PayAsYouGoRules()
    {
        try
        {
            return Ok(await _db.PayAsYouGoRules.OrderBy(item => item.Action).ToListAsync());
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPost("admin/paygo-rules")]
    public async Task<IActionResult> SavePayAsYouGoRule([FromBody] PayAsYouGoRuleRequest request)
    {
        try
        {
            var entity = await _db.PayAsYouGoRules.FirstOrDefaultAsync(item => item.Action == request.Action);
            if (entity == null)
            {
                entity = new PayAsYouGoRule { Id = Guid.NewGuid(), Action = request.Action, CreatedAt = DateTime.UtcNow };
                _db.PayAsYouGoRules.Add(entity);
            }
            entity.IsEnabled = request.IsEnabled;
            entity.FreeUnitsPerPeriod = Math.Max(0, request.FreeUnitsPerPeriod);
            entity.UnitPrice = Math.Max(0, request.UnitPrice);
            entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant();
            entity.PeriodKey = string.IsNullOrWhiteSpace(request.PeriodKey) ? "Monthly" : request.PeriodKey.Trim();
            entity.RequirePaymentBeforeAction = request.RequirePaymentBeforeAction;
            entity.Description = request.Description ?? string.Empty;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(entity);
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpGet("admin/paygo-charges")]
    public async Task<IActionResult> PayAsYouGoCharges()
    {
        try
        {
            var rows = await _db.PayAsYouGoCharges.OrderByDescending(item => item.CreatedAt).Take(250).ToListAsync();
            return Ok(rows.Select(item => new { item.Id, action = item.Action.ToString(), status = item.Status.ToString(), item.UserId, item.EmployerId, item.RelatedEntityId, item.Amount, item.Currency, item.PeriodKey, item.FailureReason, item.CreatedAt, item.CompletedAt }));
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPost("admin/activate")]
    public async Task<IActionResult> ManualActivate([FromBody] ManualSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptions.ActivateAsync(request.EmployerId, request.PlanId, "AdminManual", request.PaymentTransactionId, CurrentUserId(), request.DurationDays, request.Notes, HttpContext.RequestAborted);
            if (request.PaymentTransactionId.HasValue)
            {
                var transaction = await _db.PaymentTransactions.FirstOrDefaultAsync(item => item.Id == request.PaymentTransactionId);
                if (transaction != null) { transaction.Status = PaymentTransactionStatus.Successful; transaction.CompletedAt = DateTime.UtcNow; transaction.EmployerSubscriptionId = subscription.Id; await _db.SaveChangesAsync(); }
            }
            return Ok(subscription);
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    private Guid CurrentUserId() => Guid.TryParse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
    private static string PeriodKey(string period) => period.Equals("Daily", StringComparison.OrdinalIgnoreCase)
        ? DateTime.UtcNow.ToString("yyyy-MM-dd")
        : DateTime.UtcNow.ToString("yyyy-MM");
}

public record SubscriptionCheckoutRequest(Guid EmployerId, Guid PlanId, PaymentProviderType? Provider, Dictionary<string, string>? PayerDetails);
public record ManualSubscriptionRequest(Guid EmployerId, Guid PlanId, int? DurationDays, Guid? PaymentTransactionId, string? Notes);
public record PaymentProviderConfigRequest(PaymentProviderType Provider, string DisplayName, bool IsEnabled, bool IsSandbox, string ApiBaseUrl, string ClientId, string ClientSecret, string? BusinessShortCode, string? PassKey, string? ReceiverAccount, string? CallbackUrl, string CallbackVerificationToken, string Currency, string PromptFieldsJson);
public record PayAsYouGoRuleRequest(PayAsYouGoAction Action, bool IsEnabled, int FreeUnitsPerPeriod, decimal UnitPrice, string Currency, string PeriodKey, bool RequirePaymentBeforeAction, string? Description);
public record PayAsYouGoRecordRequest(PayAsYouGoAction Action, Guid? EmployerId, Guid? RelatedEntityId, Dictionary<string, string>? PayerDetails);
