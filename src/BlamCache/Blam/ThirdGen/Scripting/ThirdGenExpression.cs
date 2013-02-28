using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Scripting
{
    public class ThirdGenExpression : IExpression
    {
        private DatumIndex _nextIndex;
        private int _stringTableOffset;

        public ThirdGenExpression(StructureValueCollection values, ushort index, StringTableReader stringReader)
        {
            Load(values, index, stringReader);
        }

        internal void ResolveReferences(ExpressionTable allExpressions)
        {
            if (_nextIndex.IsValid)
                Next = allExpressions.FindExpression(_nextIndex);
        }

        internal void ResolveStrings(CachedStringTable requestedStrings)
        {
            StringValue = requestedStrings.GetString(_stringTableOffset);
        }

        public DatumIndex Index { get; private set; }
        public ushort Opcode { get; private set; }
        public ExpressionType Type { get; private set; }
        public short ValueType { get; private set; }
        public string StringValue { get; private set; }
        public uint Value { get; private set; }
        public short LineNumber { get; private set; }
        public IExpression Next { get; private set; }

        private void Load(StructureValueCollection values, ushort index, StringTableReader stringReader)
        {
            ushort salt = (ushort)values.GetInteger("datum index salt");
            Index = new DatumIndex(salt, index);

            Opcode = (ushort)values.GetInteger("opcode");
            ValueType = (short)values.GetInteger("value type");
            Type = (ExpressionType)values.GetInteger("expression type");
            _nextIndex = new DatumIndex(values.GetInteger("next expression index"));
            _stringTableOffset = (int)values.GetInteger("string table offset");
            Value = values.GetInteger("value");
            LineNumber = (short)values.GetInteger("source line");

            stringReader.RequestString(_stringTableOffset);
        }
    }
}
