﻿using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Enums;

namespace BancoBr.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CampoCNABAttribute : Attribute
    {
        public bool Desabilitado { get; }
        public int Posicao { get; }
        public int Tamanho { get; }
        public char? CharPreenchimento { get; }
        public AlinhamentoPreenchimentoEnum AlinhamentoPreenchimento { get; set; }
        public string Formatacao { get; }

        public CampoCNABAttribute(int posicao, int tamanho)
        {
            Posicao = posicao;
            Tamanho = tamanho;
        }

        public CampoCNABAttribute(int posicao, int tamanho, string formatacao)
        {
            Posicao = posicao;
            Tamanho = tamanho;
            Formatacao = formatacao;
        }

        public CampoCNABAttribute(int posicao, int tamanho, char charPreenchimento)
        {
            Tamanho = tamanho;
            Posicao = posicao;
            CharPreenchimento = charPreenchimento;
        }

        public CampoCNABAttribute(int posicao, int tamanho, char charPreenchimento, AlinhamentoPreenchimentoEnum alinhamentoPreenchimento)
        {
            Tamanho = tamanho;
            Posicao = posicao;
            CharPreenchimento = charPreenchimento;
            AlinhamentoPreenchimento = alinhamentoPreenchimento;
        }

        public CampoCNABAttribute(int posicao, int tamanho, char charPreenchimento, string formatacao)
        {
            Tamanho = tamanho;
            Posicao = posicao;
            CharPreenchimento = charPreenchimento;
            Formatacao = formatacao;
        }

        public CampoCNABAttribute(bool desabilitado)
        {
            Desabilitado = desabilitado;
        }
    }
}
