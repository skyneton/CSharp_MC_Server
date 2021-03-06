using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using NiaBukkit.API.Entities;
using NiaBukkit.API.Util;
using NiaBukkit.Network;
using NiaBukkit.Network.Protocol.Play;

namespace NiaBukkit.API
{
    public class Bukkit
    {
	    /// <summary>플러그인 관련 클래스를 가져옵니다.</summary>
        public static readonly PluginManager PluginManager = new();
		
	    /// <summary>콘솔 출력을 가져옵니다.</summary>
		public static readonly ConsoleSender ConsoleSender = new();

        internal static MinecraftServer MinecraftServer;
        
        internal static readonly ConcurrentDictionary<Uuid, Player> Players = new();
        internal static readonly ConcurrentDictionary<Uuid, Entity> Entities = new();
        public static ReadOnlyCollection<Player> OnlinePlayers => new(Players.Values.ToList());

		/// <summary>서버 파일이 있는 디렉터리 위치를 가져옵니다.</summary>
        public static string ServerPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		public static World.World MainWorld = new World.World("world");

		internal static void AddPlayer(Player player)
		{
			lock (Players)
			{
				player.World.Players.Add(player);
				player.World.Entities.Add(player);
				Players.TryAdd(player.Uuid, player);
				Entities.TryAdd(player.Uuid, player);
			}
		}

		internal static void RemovePlayer(Player player)
		{
			lock (Players)
			{
				player.World.Players.Remove(player);
				player.World.Entities.Remove(player);
				Players.Remove(player.Uuid, out _);
				Entities.Remove(player.Uuid, out _);
			}
		}

		public static void BroadcastMessage(string message) => BroadcastMessage(Uuid.RandomUuid(), message);

		internal static void BroadcastMessage(Uuid sender, string message)
		{
			ConsoleSender.SendMessage(message);
			MinecraftServer.Broadcast(new PlayOutChatMessage(message, ChatMessageType.Chat, sender));
		}
    }
}