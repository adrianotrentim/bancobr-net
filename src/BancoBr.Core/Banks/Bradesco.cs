using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.Core.Banks
{
    public class Bradesco : IBank
    {
        public Bradesco()
        {
            SwiftCode = "BBDEBRSPSPO";
            BankCode = "237";
            Name = "Banco Bradesco SA";
        }

        public string SwiftCode { get; set; }
        public string BankCode { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"[{SwiftCode}] ({BankCode}) {Name}";
        }
    }
}
