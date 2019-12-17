using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
	public static class CommandSerialization
	{
		public static void SerializeCommand(PokeCommand command, Stream stream)
		{
			var type = (byte)command.Type;
			stream.WriteByte(type);
			command.Serialize(stream);
		}

		public static PokeCommand DeserializeCommand(Stream stream)
		{
			var commandType = (PokeCommandType)stream.ReadByte();
			PokeCommand command;
			switch (commandType)
			{
				case PokeCommandType.Test:
					command = new TestCommand();
					break;
				case PokeCommandType.Memory:
					command = new MemoryCommand();
					break;
				default:
					throw new NotImplementedException("The suspected command type has not been implemented yet.");
			}
			command.Deserialize(stream);
			return command;
		}
	}
}
