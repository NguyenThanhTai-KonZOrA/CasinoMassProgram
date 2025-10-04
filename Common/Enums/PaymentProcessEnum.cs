using System.ComponentModel;

namespace Common.Enums
{
    public enum PaymentProcessEnum
    {
        [Description("Inprocess")]
        Inprocess,
        [Description("Paid")]
        Paid,
        [Description("Voided")]
        Voided,
        [Description("Falied")]
        Falied
    }
}
