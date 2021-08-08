using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace Blamite.Blam.Scripting.Compiler
{
    public static class CompilerExtensions
    {
        public static string GetTextSanitized(this RuleContext context)
        {
            string text = context.GetText();
            return text.Trim('"').ToLowerInvariant();
        }

        public static string GetTextSanitized(this ITerminalNode context)
        {
            string text = context.GetText();
            return text.Trim('"').ToLowerInvariant();
        }

        public static uint GetCorrectTextPosition(this ParserRuleContext context, IEnumerable<int> missingCarriageReturnPositions)
        {
            int wrongIndex = context.Start.StartIndex;
            int fix = missingCarriageReturnPositions.Where(i => i < wrongIndex).Count();
            return (uint)(wrongIndex + fix);
        }
    }
}
