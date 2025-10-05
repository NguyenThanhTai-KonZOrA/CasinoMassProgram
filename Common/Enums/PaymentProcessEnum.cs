using System.ComponentModel;

namespace Common.Enums
{
    public enum PaymentProcessEnum
    {
        [Description("Inprocess")]
        Inprocess,
        [Description("Pending")]
        Pending,
        [Description("Paid")]
        Paid,
        [Description("Voided")]
        Voided,
        [Description("Falied")]
        Falied
    }
}
