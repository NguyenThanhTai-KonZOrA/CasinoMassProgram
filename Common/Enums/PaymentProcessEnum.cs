using System.ComponentModel;

namespace Common.Enums
{
    public enum PaymentProcessEnum
    {
        [Description("Inprocess")]
        Inprocess,
        [Description("Voided")]
        Voided,
        [Description("Falied")]
        Falied
    }
}
