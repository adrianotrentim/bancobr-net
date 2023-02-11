using BancoBr.CNAB;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Xunit;

namespace BancoBr.Tests
{
    public class CriaBancoTest
    {
        [Fact]
        public void CriaBancoBradesco()
        {
            var cnab = new ArquivoCNAB(Bancos.BradescoSA);

            Assert.Equal(237, cnab.Banco.Codigo);

            var lote = cnab.NovoLotePagamento();
            
            var titulo1 = new Titulo();
            lote.NovoPagamento(titulo1);

            var titulo2 = new Titulo();
            lote.NovoPagamento(titulo2);

            Assert.Equal(237, lote.Registros.FirstOrDefault().Banco.Codigo);

            Assert.Equal(1, cnab.Trailer.QuantidadeLotes);
            Assert.Equal(4, lote.Trailer.QuantidadeRegistros);
            Assert.Equal(6, cnab.Trailer.QuantidadeRegistros);
        }

        [Fact]
        public void CriaBancoSantander()
        {
            var cnab = new ArquivoCNAB(Bancos.Santander);

            Assert.Equal(33, cnab.Banco.Codigo);
        }
    }
}
