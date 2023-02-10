using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.Core
{
    public interface IBank
    {
        /// <summary>
        /// Each bank has a unique SWIFT code
        /// </summary>
        string SwiftCode { get; set; }

        /// <summary>
        /// Febraban bank code 
        /// </summary>
        string BankCode { get; set; }

        /// <summary>
        /// Bank name
        /// </summary>
        string Name { get; set; }
    }
}
