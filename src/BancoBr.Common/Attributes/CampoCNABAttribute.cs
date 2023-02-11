using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CampoCNABAttribute : Attribute
    {
        public CampoCNABAttribute(double posicao, int tamanho)
        {
            
        }

        public CampoCNABAttribute(double posicao, int tamanho, string charPreenchimento)
        {

        }

        public CampoCNABAttribute(bool disable)
        {

        }
    }
}
