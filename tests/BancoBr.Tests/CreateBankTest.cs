using BancoBr.Core.Banks;
using Xunit;

namespace BancoBr.Tests
{
    public class CreateBankTest
    {
        [Fact]
        public void CreateBradescoBank()
        {
            var bank = new Bradesco();

            Assert.Equal("BBDEBRSPSPO", bank.SwiftCode);
            Assert.Equal("237", bank.BankCode);
        }
    }
}
