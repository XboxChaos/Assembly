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
            ushort salt = (ushort)values.GetNumber("datum index salt");
            Index = new DatumIndex(salt, index);

            Opcode = (ushort)values.GetNumber("opcode");
            ValueType = (short)values.GetNumber("value type");
            Type = (ExpressionType)values.GetNumber("expression type");
            _nextIndex = new DatumIndex(values.GetNumber("next expression index"));
            _stringTableOffset = (int)values.GetNumber("string table offset");
            Value = values.GetNumber("value");
            LineNumber = (short)values.GetNumber("source line");

            stringReader.RequestString(_stringTableOffset);
        }
    }
}
