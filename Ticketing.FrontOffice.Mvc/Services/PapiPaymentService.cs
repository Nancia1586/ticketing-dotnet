using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Ticketing.FrontOffice.Mvc.Models;

namespace Ticketing.FrontOffice.Mvc.Services
{
    public class PapiPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PapiPaymentService> _logger;

        public PapiPaymentService(HttpClient httpClient, IConfiguration configuration, ILogger<PapiPaymentService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PapiPaymentResponseData?> CreatePaymentLinkAsync(PapiPaymentRequest request)
        {
            var apiKey = _configuration["PapiSettings:ApiKey"];
            var baseUrl = _configuration["PapiSettings:BaseUrl"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Papi API Key is not configured.");
            }

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("Papi BaseUrl is not configured.");
            }

            // Validate required fields
            if (request.Amount < 300)
            {
                throw new ArgumentException("Amount must be at least 300 MGA");
            }

            if (string.IsNullOrEmpty(request.ClientName))
            {
                throw new ArgumentException("ClientName is required");
            }

            if (string.IsNullOrEmpty(request.Reference))
            {
                throw new ArgumentException("Reference is required");
            }

            if (string.IsNullOrEmpty(request.Description) || request.Description.Length > 255)
            {
                throw new ArgumentException("Description is required and must be max 255 characters");
            }

            if (string.IsNullOrEmpty(request.NotificationUrl))
            {
                throw new ArgumentException("NotificationUrl is required");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Token", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(request, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                _logger.LogInformation("Calling Papi API: {Url}", baseUrl);
                _logger.LogDebug("Request: {Request}", json);

                var response = await _httpClient.PostAsync(baseUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<PapiPaymentResponse>(responseContent, options);
                    if (apiResponse?.Data != null && !string.IsNullOrEmpty(apiResponse.Data.PaymentLink))
                    {
                        _logger.LogInformation("Payment link created successfully. Reference: {Reference}, PaymentLink: {PaymentLink}", 
                            apiResponse.Data.PaymentReference, apiResponse.Data.PaymentLink);
                        return apiResponse.Data;
                    }
                    else
                    {
                        _logger.LogError("Papi API returned success but data is null or PaymentLink is empty. Response: {Content}", responseContent);
                        return null;
                    }
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<PapiErrorResponse>(responseContent, options);
                        _logger.LogError("Papi API Error: {StatusCode} - Code: {Code}, Message: {Message}", 
                            response.StatusCode, 
                            errorResponse?.Error?.Code ?? "UNKNOWN", 
                            errorResponse?.Error?.Message ?? responseContent);
                    }
                    catch
                    {
                        _logger.LogError("Papi API Error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Papi API");
                return null;
            }
        }

        public PapiPaymentRequest PrepareRequest(decimal amount, string clientName, string email, string reference, string description, string? phoneNumber = null, string? provider = null, HttpContext? httpContext = null)
        {
            // Get URLs from configuration
            var successUrl = _configuration["PapiSettings:SuccessUrl"];
            var failureUrl = _configuration["PapiSettings:FailureUrl"];
            var notificationUrl = _configuration["PapiSettings:NotificationUrl"];

            // Build absolute URLs if they are relative (according to Papi docs: URLs must be http(s)://)
            if (httpContext != null)
            {
                var request = httpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";

                if (!string.IsNullOrEmpty(successUrl) && !Uri.IsWellFormedUriString(successUrl, UriKind.Absolute))
                {
                    successUrl = successUrl.StartsWith("/") ? baseUrl + successUrl : baseUrl + "/" + successUrl;
                }

                if (!string.IsNullOrEmpty(failureUrl) && !Uri.IsWellFormedUriString(failureUrl, UriKind.Absolute))
                {
                    failureUrl = failureUrl.StartsWith("/") ? baseUrl + failureUrl : baseUrl + "/" + failureUrl;
                }

                if (!string.IsNullOrEmpty(notificationUrl) && !Uri.IsWellFormedUriString(notificationUrl, UriKind.Absolute))
                {
                    notificationUrl = notificationUrl.StartsWith("/") ? baseUrl + notificationUrl : baseUrl + "/" + notificationUrl;
                }
            }

            // Ensure notificationUrl is not empty (required field)
            if (string.IsNullOrEmpty(notificationUrl))
            {
                throw new InvalidOperationException("NotificationUrl is required and must be configured in appsettings.json");
            }

            // Append order/reference to successUrl as query parameter so that the app can load the correct reservation
            if (!string.IsNullOrEmpty(successUrl) && !string.IsNullOrEmpty(reference))
            {
                var separator = successUrl.Contains("?") ? "&" : "?";
                successUrl = $"{successUrl}{separator}reference={Uri.EscapeDataString(reference)}";
            }

            return new PapiPaymentRequest
            {
                Amount = amount,
                ClientName = clientName,
                PayerEmail = email,
                PayerPhone = phoneNumber,
                Reference = reference,
                Description = description.Length > 255 ? description.Substring(0, 255) : description,
                SuccessUrl = successUrl,
                FailureUrl = failureUrl,
                NotificationUrl = notificationUrl,
                ValidDuration = _configuration.GetValue<int?>("PapiSettings:ValidDuration"),
                Provider = provider,
                IsTestMode = _configuration.GetValue<bool>("PapiSettings:IsTestMode", false),
                TestReason = _configuration.GetValue<bool>("PapiSettings:IsTestMode", false) 
                    ? _configuration["PapiSettings:TestReason"] 
                    : null
            };
        }
    }
}
