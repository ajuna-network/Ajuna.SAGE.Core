using System.ComponentModel;

namespace Ajuna.SAGE.Core.Model
{
    public interface ITransitionFee
    {
        uint Fee { get; }
    }

    public class TransitionFee : ITransitionFee
    {
        public uint Fee { get; private set; }

        public TransitionFee(uint fee)
        {
            Fee = fee;
        }

    }
}