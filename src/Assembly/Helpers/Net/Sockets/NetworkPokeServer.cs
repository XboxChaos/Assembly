using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
//using Mono.Nat;

namespace Assembly.Helpers.Net.Sockets
{
	/// <summary>
	/// Network poking server.
	/// </summary>
	public class NetworkPokeServer
	{
		private Socket _listener;
		private readonly List<Socket> _clients = new List<Socket>();

		// TODO: Should we make it possible to set the port number somehow?
		private static int Port = 19002;

		private static string UpnpDescription = "Assembly Network Poking";

		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkPokeServer"/> class.
		/// A server will be created on the local machine on port 19002.
		/// </summary>
		public NetworkPokeServer()
		{
			// Discover UPnP support
			//NatUtility.DeviceFound += DeviceFound;
			//NatUtility.StartDiscovery();
		}

		public bool Listen()
		{
			try
			{
				var hostIp = IPAddress.Any;
				var hostEndpoint = new IPEndPoint(hostIp, Port);
				_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				// Bind to our local endpoint
				_listener.Bind(hostEndpoint);
				_listener.Listen(128); // Listen with a pending connection queue size of 128
				return true;
			}
			catch (SocketException)
			{

			}
			return false;
		}

		/// <summary>
		/// Updates the state of the server and waits for a command to become available.
		/// The first command that is available will be passed into a handler.
		/// </summary>
		/// <param name="handler">The <see cref="IPokeCommandHandler"/> to handle the command with.</param>
		public void ReceiveCommand(IPokeCommandHandler handler)
		{
			// Loop until a command is processed
			while (true)
			{
				// Duplicate our clients list for use with Socket.Select()
				List<Socket> readyClients;
				List<Socket> failedClients = new List<Socket>();
				lock (_clients)
				{
					readyClients = new List<Socket>(_clients);
				}

				// The listener socket is "readable" when a client is ready to be accepted
				readyClients.Add(_listener);

				// Wait for either a command to become available in a client,
				// or a client to be ready to connect
				Socket.Select(readyClients, null, null, -1);
				foreach (var socket in readyClients)
				{
					if (socket != _listener)
					{
						try
						{
							// Command available
							using (var stream = new NetworkStream(socket, false))
							{
								var command = CommandSerialization.DeserializeCommand(stream);
								SendCommandToAll(command);
								command.Handle(handler);
							}
						}
						catch (IOException)
						{
							failedClients.Add(socket);
						}
						break; // Only process one command at a time
					}
					else
					{
						// Client ready to connect
						var client = _listener.Accept();
						ConnectClient(client);
					}
				}
				lock (_clients)
				{
					foreach (var socket in failedClients)
					{
						socket.Close();
						_clients.Remove(socket);
					}
				}

			}
		}

		/// <summary>
		/// Sends a command to all connected clients.
		/// </summary>
		/// <param name="command">The command to send.</param>
		public void SendCommandToAll(PokeCommand command)
		{
			IList<Socket> failedClients = new List<Socket>();
			lock (_clients)
			{
				foreach (var socket in _clients)
				{
					try
					{
						using (var stream = new NetworkStream(socket, false))
							CommandSerialization.SerializeCommand(command, stream);
					}
					catch (IOException)
					{
						socket.Close();
						failedClients.Add(socket);
					}
				}
				foreach (var socket in failedClients)
				{
					_clients.Remove(socket);
				}
			}
		}

		/// <summary>
		/// Connects a new client to the server.
		/// </summary>
		/// <param name="client">The client to connect.</param>
		private void ConnectClient(Socket client)
		{
			lock (_clients)
			{
				_clients.Add(client);
			}
		}

		/// <summary>
		/// Callback for when a UPnP device is found.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
//		private void DeviceFound(object sender, DeviceEventArgs e)
//		{
//			// Create a UPnP mapping for our port
//			var device = e.Device;
//			var map = new Mapping(Protocol.Tcp, Port, Port);
//			map.Description = UpnpDescription;
//			device.CreatePortMap(map);

//#if DEBUG
//			Debug.WriteLine("UPnP found device: " + device.GetExternalIP());
//#endif
//		}
	}
}