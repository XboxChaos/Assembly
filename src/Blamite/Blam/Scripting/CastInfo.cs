using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting
{
    public class CastInfo
    {
        public string To { get; private set; }
        public Boolean CastOnly { get; private set; }
        public List<string> From { get; private set; }

        public CastInfo(string to, Boolean castOnly, List<string> from)
        {
            To = to;
            CastOnly = castOnly;
            From = from;
        }
    }
}
