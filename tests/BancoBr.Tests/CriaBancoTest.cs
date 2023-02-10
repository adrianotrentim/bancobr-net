using Xunit;

namespace BancoBr.Tests
{
    public class CriaBancoTest
    {
        [Fact]
        public void CriaBancoBradesco()
        {
            var bank = new CNAB.Bradesco.Banco();

            Assert.Equal(237, bank.Codigo);
        }

        [Fact]
        public void CriaBancoSantander()
        {
            var bank = new CNAB.Bradesco.Banco();

            Assert.Equal(33, bank.Codigo);
        }
    }
}
