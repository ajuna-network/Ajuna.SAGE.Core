﻿using System.ComponentModel;

namespace Ajuna.SAGE.Core.Model
{
    public interface ITransitioFee
    {
        uint Fee { get; }
    }

    public class TransitioFee : ITransitioFee
    {
        public uint Fee { get; private set; }

        public TransitioFee(uint fee)
        {
            Fee = fee;
        }

    }
}