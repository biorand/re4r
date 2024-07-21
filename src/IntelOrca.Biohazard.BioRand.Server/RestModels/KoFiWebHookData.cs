using System;

namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class KoFiWebHookData
    {
        public Guid VerificationToken { get; set; }
        public Guid MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public bool IsPublic { get; set; }
        public string? FromName { get; set; }
        public string? Message { get; set; }
        public string Amount { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Email { get; set; }
        public string Currency { get; set; } = "";
        public bool IsSubscriptionPayment { get; set; }
        public bool IsFirstSubscriptionPayment { get; set; }
        public string KofiTransactionId { get; set; } = "";
        public object? ShopItems { get; set; }
        public string? TierName { get; set; }
        public object? Shipping { get; set; }
    }
}
