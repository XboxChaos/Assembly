using System;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Blam.Scripting.Compiler
{
    public class TypeStack
    {
        private readonly Stack<string> _expectedTypes = new Stack<string>();
        private readonly ScriptCompilerLogger _logger;
        private readonly bool _debug;

        public TypeStack(ScriptCompilerLogger logger, bool debug)
        {
            _logger = logger;
            _debug = debug;
        }

        public void PushType(string type)
        {
            _expectedTypes.Push(type);
            if (_debug)
            {
                int index = _expectedTypes.Count - 1;
                _logger.TypeStack(type, index, StackAction.Push);
            }
        }

        public void PushTypes(params string[] types)
        {
            var reverse = types.Reverse();
            foreach (string type in reverse)
            {
                PushType(type);
            }
        }

        public void PushTypes(string type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                PushType(type);
            }
        }

        public string PopType()
        {
            if (_expectedTypes.Count > 0)
            {
                string type = _expectedTypes.Pop();
                if (_debug)
                {
                    int index = _expectedTypes.Count;
                    _logger.TypeStack(type, index, StackAction.Pop);
                }
                return type;
            }
            else
            {
                throw new InvalidOperationException("Failed to pop a value type. The expected types stack was empty.");
            }
        }
    }
}
