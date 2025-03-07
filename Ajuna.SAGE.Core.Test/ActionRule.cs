﻿using Ajuna.SAGE.Core.Model;

namespace Ajuna.SAGE.Core.Test
{
    public struct ActionRule : ITransitionRule
    {
        public byte RuleType { get; private set; }

        public byte RuleOp { get; private set; }

        public byte[] RuleValue { get; private set; }

        public ActionRule(ActionRuleType type, ActionRuleOp operation, uint value)
        {
            RuleType = Convert.ToByte(type);
            RuleOp = Convert.ToByte(operation);
            RuleValue = BitConverter.GetBytes(value);
        }
    }
}
