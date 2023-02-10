using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CNABAttribute : Attribute
    {
        public CNABAttribute(int posicao, int tamanho)
        {
            
        }
    }
}
