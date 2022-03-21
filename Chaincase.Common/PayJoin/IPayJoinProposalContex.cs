using BTCPayServer.BIP78.Receiver;

namespace Chaincase.Common.PayJoin
{
    public interface IPayJoinProposalContex
    {
        void SetPaymentRequest(PayjoinPaymentRequest paymentRequest);
    }
}