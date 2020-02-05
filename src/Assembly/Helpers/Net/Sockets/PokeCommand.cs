using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
    public abstract class PokeCommand
    {
        protected PokeCommand(PokeCommandType type)
        {
            Type = type;
        }

        public abstract void Deserialize(Stream stream);

        public abstract void Serialize(Stream stream);

        public abstract void Handle(IPokeCommandHandler handle);

        public PokeCommandType Type { get; private set; }
    }
}
