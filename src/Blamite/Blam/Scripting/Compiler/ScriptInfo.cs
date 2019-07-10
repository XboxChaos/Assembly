using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class ScriptInfo
    {
        public string Name { get; private set; }
        public string ScriptType { get; private set; }
        public string ReturnType { get; private set; }
        public List<ParameterInfo> Parameters { get; private set; }

        public ScriptInfo(BS_ReachParser.ScriDeclContext context)
        {
            Name = context.ID().GetText();
            ScriptType = context.SCRIPTTYPE().GetText();
            ReturnType = context.retType().GetText();
            Parameters = new List<ParameterInfo>();

            if (context.scriptParams() != null)
            {
                string[] names = context.scriptParams().ID().Select(n => n.GetText()).ToArray();
                string[] valTypes = context.scriptParams().VALUETYPE().Select(v => v.GetText()).ToArray();
                // extract strings from the context

                if (names.Count() != valTypes.Count())
                    throw new Exception($"Error while creating parameter information for Script: \"{Name}\" - Mismatched parameter arrays.");

                // create parameters from the extracted strings
                for (int i = 0; i < names.Count(); i++)
                {
                    var param = new ParameterInfo(names[i], valTypes[i]);
                    Parameters.Add(param);
                }
            }                
        }

        //public void Print()
        //{
        //    Console.WriteLine($"Script - Name: {Name} ScrType: {ScriptType} RetType: {ReturnType}");
        //    if(Parameters.Count > 0)
        //    {
        //        Console.Write("Parameters:");
        //        foreach (string param in Parameters)
        //            Console.Write(" " + param);
        //        Console.WriteLine();
        //    }
        //}
    }
}
