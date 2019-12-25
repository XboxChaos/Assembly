using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.ObjectModel;
using Mono.Nat;

namespace Assembly.Helpers.Net.Sockets
{
	/// <summary>
	/// Network poking server.
	/// </summary>
	public class NetworkPokeServer
	{
		private Socket _listener;
		private readonly List<Socket> _clients = new List<Socket>();
		private int _port;

		public event EventHandler<ClientEventArgs> ClientConnected;
		public event EventHandler<ClientEventArgs> ClientDisconnected;

		private const string UpnpDescription = "Assembly Network Poking";

		public void Listen(IPEndPoint endpoint)
		{
			_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			// Bind to our local endpoint
			_listener.Bind(endpoint);
			_port = endpoint.Port;
			_listener.Listen(128);
			NatUtility.DeviceFound += DeviceFound;
			NatUtility.StartDiscovery();
		}

		/// <summary>
		/// Updates the state of the server and waits for a command to become available.
		/// The first command that is available will be passed into a handler.
		/// </summary>
		/// <param name="handler">The <see cref="IPokeCommandHandler"/> to handle the command with.</param>
		public bool ReceiveCommand(IPokeCommandHandler handler)
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
							if (command != null)
							{
								SendCommandToAll(command);
								command.Handle(handler);
							}
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

			foreach (var socket in failedClients)
			{
				RemoveClient(socket);
				_clients.Remove(socket);
				socket.Close();
			}
			return true;
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
						using (var stream = new BufferedStream(new NetworkStream(socket, false)))
						{
							CommandSerialization.SerializeCommand(command, stream);
						}
					}
					catch (IOException)
					{
						failedClients.Add(socket);
					}
				}
				foreach (var socket in failedClients)
				{
					_clients.Remove(socket);
					socket.Close();
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
			ClientConnected(this, new ClientEventArgs(client.RemoteEndPoint.ToString()));
		}

		private void RemoveClient(Socket client)
		{
			ClientDisconnected(this, new ClientEventArgs(client.RemoteEndPoint.ToString()));
			lock (_clients)
			{
				_clients.Remove(client);
				client.Close();
			}
		}

		public void Close()
		{
			_listener.Close();
			lock (_clients)
			{
				foreach (var socket in _clients)
				{
					socket.Close();
				}
			}
		}

		// <summary>
		// Callback for when a UPnP device is found.
		// </summary>
		// <param name="sender"></param>
		// <param name="e"></param>
		private void DeviceFound(object sender, DeviceEventArgs e)
		{
			// Create a UPnP mapping for our port
			var device = e.Device;
			var map = new Mapping(Protocol.Tcp, _port, _port);
			map.Description = UpnpDescription;
			device.CreatePortMap(map);

#if DEBUG
			Debug.WriteLine("UPnP found device: " + device.GetExternalIP());
#endif
		}
	}
}