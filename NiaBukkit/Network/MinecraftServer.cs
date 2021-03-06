using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NiaBukkit.API;
using NiaBukkit.API.Util;
using NiaBukkit.API.Config;
using NiaBukkit.API.Entities;
using NiaBukkit.API.Threads;
using NiaBukkit.API.World;
using NiaBukkit.Network.Protocol;

namespace NiaBukkit.Network
{
    public class MinecraftServer
    {
        private const int ThreadEmptyDelay = 500;
        public const string MinecraftKey = "minecraft:";
        private const int ThreadDelay = 50;
        // public ProtocolVersion Protocol { get; internal set; } = ProtocolVersion.v15w33b;
        public ProtocolVersion Protocol { get; internal set; } = ProtocolVersion.v1_12_2;
        
        private TcpListener _listener;

        private readonly ConcurrentBag<NetworkManager> _networkManagers = new();
        
        public bool IsAvailable { get; private set; }

        public const string ServerId = "";

        internal readonly SelfCryptography Cryptography = new();

        private readonly ThreadFactory _threadFactory = new();

        internal MinecraftServer()
        {
			Init();
			SocketStart();
        }

        public void Stop(string stopMessage = "Server Closed")
        {
            foreach (var networkManager in _networkManagers)
                networkManager.Kick(stopMessage);

            _threadFactory.KillAll();
        }
		
		private void Init()
		{
            IsAvailable = true;
			
            _listener = new TcpListener(IPAddress.Any, ServerProperties.Port)
            {
                Server =
                {
                    NoDelay = true,
                    SendTimeout = 500
                }
            };
		}
		
		private void SocketStart()
		{
            try
            {
                _listener.Start();
            }
            catch (SocketException)
            {
                Bukkit.ConsoleSender.SendWarnMessage("Failed to start the minecraft server");
                throw;
            }

            _listener.BeginAcceptTcpClient(AcceptSocket, null);
            _threadFactory.LaunchThread(new Thread(ClientUpdateWorker), false).Name = "Client Thread";

            _threadFactory.LaunchThread(new Thread(WorldThreadManager.Worker), false).Name = "World Generator";
            _threadFactory.LaunchThread(new Thread(EntityThreadManager.Worker), false).Name = "Entity Thread";
        }

        private void AcceptSocket(IAsyncResult result)
        {
            try
            {
                _networkManagers.Add(new NetworkManager(_listener.EndAcceptTcpClient(result)));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            _listener.BeginAcceptTcpClient(AcceptSocket, null);
        }

        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: Enumerator[NiaBukkit.Network.NetworkManager]")]
        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: NiaBukkit.Network.NetworkManager[]")]
        private void ClientUpdateWorker()
        {
            while (IsAvailable)
            {
                if (_networkManagers.IsEmpty)
                {
                    Thread.Sleep(ThreadEmptyDelay);
                    continue;
                }
                
                var destroy = new Queue<NetworkManager>();
                ClientForEachUpdate(destroy);
                ClientForEachDestroy(destroy);
                
                Thread.Sleep(ThreadDelay);
            }
        }

        private void ClientForEachUpdate(Queue<NetworkManager> destroy)
        {
            foreach (var networkManager in _networkManagers)
            {
                if (!(networkManager?.IsAvailable ?? false))
                {
                    destroy.Enqueue(networkManager);
                    continue;
                }
                networkManager.Update();
            }
        }

        private void ClientForEachDestroy(Queue<NetworkManager> destroy)
        {
            while(destroy.Count > 0)
            {
                var networkManager = destroy.Dequeue();
            
                networkManager.Close();
                _networkManagers.Remove(networkManager);
            }
        }

        public string GetServerModName()
        {
            return "NiaBukkit";
        }

        internal static void BroadcastInWorld(Player sender, World world, IPacket packet, bool me = true)
        {
            foreach (var player in world.Players)
            {
                if(player == sender && !me) continue;
                ((EntityPlayer) player).NetworkManager.SendPacket(packet);
            }
        }

        internal static void Broadcast(IPacket packet)
        {
            foreach (var player in Bukkit.OnlinePlayers)
            {
                ((EntityPlayer) player).NetworkManager.SendPacket(packet);
            }
        }
    }
}