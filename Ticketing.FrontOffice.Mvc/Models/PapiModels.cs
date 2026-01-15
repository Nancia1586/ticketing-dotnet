using System.Text.Json.Serialization;

namespace Ticketing.FrontOffice.Mvc.Models
{
    public class PapiPaymentRequest
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; } = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("successUrl")]
        public string? SuccessUrl { get; set; }

        [JsonPropertyName("failureUrl")]
        public string? FailureUrl { get; set; }

        [JsonPropertyName("notificationUrl")]
        public string NotificationUrl { get; set; } = string.Empty;

        [JsonPropertyName("validDuration")]
        public int? ValidDuration { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; } // MVOLA, AIRTEL_MONEY, ORANGE_MONEY, BRED

        [JsonPropertyName("payerEmail")]
        public string? PayerEmail { get; set; }

        [JsonPropertyName("payerPhone")]
        public string? PayerPhone { get; set; }

        [JsonPropertyName("testReason")]
        public string? TestReason { get; set; }

        [JsonPropertyName("isTestMode")]
        public bool IsTestMode { get; set; } = false;
    }

    public class PapiPaymentResponseData
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("linkCreationDateTime")]
        public long LinkCreationDateTime { get; set; }

        [JsonPropertyName("linkExpirationDateTime")]
        public long LinkExpirationDateTime { get; set; }

        [JsonPropertyName("paymentLink")]
        public string PaymentLink { get; set; } = string.Empty;

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; } = string.Empty;

        [JsonPropertyName("paymentReference")]
        public string PaymentReference { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("successUrl")]
        public string? SuccessUrl { get; set; }

        [JsonPropertyName("failureUrl")]
        public string? FailureUrl { get; set; }

        [JsonPropertyName("notificationUrl")]
        public string NotificationUrl { get; set; } = string.Empty;

        [JsonPropertyName("payerEmail")]
        public string? PayerEmail { get; set; }

        [JsonPropertyName("payerPhone")]
        public string? PayerPhone { get; set; }

        [JsonPropertyName("notificationToken")]
        public string NotificationToken { get; set; } = string.Empty;

        [JsonPropertyName("testReason")]
        public string? TestReason { get; set; }

        [JsonPropertyName("isTestMode")]
        public bool IsTestMode { get; set; }
    }

    public class PapiPaymentResponse
    {
        [JsonPropertyName("data")]
        public PapiPaymentResponseData? Data { get; set; }
    }

    public class PapiErrorResponse
    {
        [JsonPropertyName("error")]
        public PapiError? Error { get; set; }
    }

    public class PapiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class PapiNotificationPayload
    {
        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = string.Empty; // SUCCESS / PENDING / FAILED

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; } // MVOLA, AIRTEL_MONEY, ORANGE_MONEY, BRED, etc.

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("fee")]
        public decimal? Fee { get; set; }

        [JsonPropertyName("clientName")]
        public string? ClientName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("merchantPaymentReference")]
        public string? MerchantPaymentReference { get; set; }

        [JsonPropertyName("paymentReference")]
        public string PaymentReference { get; set; } = string.Empty;

        [JsonPropertyName("notificationToken")]
        public string NotificationToken { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("payerEmail")]
        public string? PayerEmail { get; set; }

        [JsonPropertyName("payerPhone")]
        public string? PayerPhone { get; set; }
    }
}
