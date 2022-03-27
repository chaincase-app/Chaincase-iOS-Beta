using System;
using System.Linq;
using System.Threading.Tasks;
using Chaincase.Common.PayJoin;
using NBitcoin;
using Xunit;


namespace Chaincase.Tests
{
    public class P2EPTests
    {
        private Coin FakeUTXO(decimal amount)
        {
            return new Coin()
            {
                Amount = new Money(amount, MoneyUnit.BTC),
                Outpoint = RandomOutpoint()
            };
        }

        private OutPoint RandomOutpoint()
        {
            return new OutPoint(RandomUtils.GetUInt256(), 0);
        }


        [Fact]
        public async Task ChooseBestUTXOsForPayjoin()
        {
            //using var tester = CreateServerTester();
            //await tester.StartAsync();
            //var network = tester.NetworkProvider.GetNetwork<BTCPayNetwork>("BTC");
            //var controller = tester.PayTester.GetService<PayJoinEndpointController>();

            //var pjWallet = new PayJoinReceiverWallet<PayJoinProposalContext>(Network.RegTest, null, new MockNotificationManager());

            //Only one utxo, so obvious result
            var network = Network.RegTest;
            //var utxos = new[] { FakeUTXO(1.0m) };
            var utxos = new[] { FakeUTXO(1.0m) };
            var paymentAmount = 0.5m;
            var otherOutputs = new[] { 0.5m };
            var inputs = new[] { 1m };
            // This fails heuristic 1: one output is smaller than any input.
            // This implies that the small output must be change. Don't make a PayJoin.
            var (selectedUTXO, pjType) = await PayJoinReceiverWallet<PayJoinProposalContext>.SelectUTXO(utxos, inputs, paymentAmount, otherOutputs);

            Assert.Equal(PayJoinCoinSelectionType.Unavailable, pjType);
            Assert.DoesNotContain(selectedUTXO, utxo => utxos.Contains(utxo));

            //no matter what here, no good selection, it seems that payment with 1 utxo generally makes payjoin coin selection unperformant
            utxos = new[] { FakeUTXO(0.3m), FakeUTXO(0.7m) };
            paymentAmount = 0.5m;
            otherOutputs = new[] { 0.5m };
            inputs = new[] { 1m };
            (selectedUTXO, pjType) = await PayJoinReceiverWallet<PayJoinProposalContext>.SelectUTXO(utxos, inputs, paymentAmount, otherOutputs);
            Assert.DoesNotContain(selectedUTXO, utxo => utxos.Contains(utxo));
            Assert.Equal(PayJoinCoinSelectionType.Unavailable, pjType);

            //when there is no change, anything works
            utxos = new[] { FakeUTXO(1), FakeUTXO(0.1m), FakeUTXO(0.001m), FakeUTXO(0.003m) };
            paymentAmount = 0.5m;
            otherOutputs = new decimal[0];
            inputs = new[] { 0.03m, 0.07m };
            (selectedUTXO, pjType) = await PayJoinReceiverWallet<PayJoinProposalContext>.SelectUTXO(utxos, inputs, paymentAmount, otherOutputs);
            Assert.Contains(selectedUTXO, utxo => utxos.Contains(utxo));
            Assert.Equal(PayJoinCoinSelectionType.AvoidsHeuristic, pjType);
        }
    }
}
