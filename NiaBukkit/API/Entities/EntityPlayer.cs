using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiaBukkit.API.Config;
using NiaBukkit.API.Threads;
using NiaBukkit.API.Util;
using NiaBukkit.API.World.Chunks;
using NiaBukkit.Network;
using NiaBukkit.Network.Protocol.Play;

namespace NiaBukkit.API.Entities
{
	public class EntityPlayer : Player
	{
		public readonly NetworkManager NetworkManager;
		public ChatMode ChatMode { get; internal set; }
		public bool ChatColor { get; internal set; }
		public int SkinPart { get; internal set; }
		public int Ping { get; internal set; } = 0;

		public readonly PlayerAbilities PlayerAbilities = new();

		private readonly List<Player> _hidePlayers = new();

		private ChunkCoord _currentChunkCoord;

		private readonly ConcurrentBag<ChunkCoord> _loadedChunk = new();

		private Location _beforeLocation;
		
		public bool IsAuthenticated { get; private set; }

		public EntityPlayer(NetworkManager networkManager, GameProfile profile, World.World world, GameMode gameMode) : base(profile, world, gameMode)
		{
			NetworkManager = networkManager;
			_beforeLocation = (Location) Location.Clone();
		}

		/**
		 * <summary>Send Message To Player</summary>
		 */
		public override void SendMessage(object message)
		{
			if (message == null)
				message = "null";
			
			SendMessage(message.ToString());
		}

		/**
		 * <summary>Send Message To Player</summary>
		 */
		public override void SendMessage(string message) => SendMessage(Uuid.RandomUuid(), ChatMessageType.Chat, message);

		public override void SendMessage(Uuid sender, string message) => SendMessage(sender, ChatMessageType.Chat, message);

		public override void SendMessage(ChatMessageType position, string message) =>
			SendMessage(Uuid.RandomUuid(), position, message);
		
		public override void SendMessage(Uuid sender, ChatMessageType position, string message)
		{
			NetworkManager.SendPacket(new PlayOutChatMessage(message, ChatMessageType.Chat, Uuid.RandomUuid()));
		}

		public bool CanSee(Player player)
		{
			return !_hidePlayers.Contains(player);
		}

		public void AddHidePlayer(Player player)
		{
			_hidePlayers.Add(player);
		}

		internal void ChunkLoaded(ChunkCoord coord)
		{
			if(!_loadedChunk.Contains(coord))
				_loadedChunk.Add(coord);
		}

		internal override void Update()
		{
			ChunkUpdate();
			PlayerChunkMoveUpdate();
			
			_beforeLocation = (Location) Location.Clone();
		}

		private void PlayerChunkMoveUpdate()
		{
			if (_loadedChunk.Contains(_currentChunkCoord)) return;
			
			if(_beforeLocation != Location)
				NetworkManager?.Teleport(_beforeLocation, Enumerable.Empty<TeleportFlags>());
		}

		private void ChunkUpdate()
		{
			if (NetworkManager == null) return;
			
			if (_currentChunkCoord != null && _currentChunkCoord.X == (int) Location.X >> 4 &&
			    _currentChunkCoord.Z == (int) Location.Z >> 4)
				return;
			_currentChunkCoord = new ChunkCoord(World, (int) Location.X >> 4, (int) Location.Z >> 4);

			var radius = ServerProperties.ViewDistance;
			if(!_loadedChunk.Contains(_currentChunkCoord))
				WorldThreadManager.AddRequireChunk(_currentChunkCoord, this);
			
			for (var x = -radius; x <= radius; x++)
			{
				for (var z = -radius; z <= radius; z++)
				{
					if(x == 0 && z == 0) continue;
					
					var target = new ChunkCoord(World, _currentChunkCoord.X + x, _currentChunkCoord.Z + z);
					if(!_loadedChunk.Contains(target))
						WorldThreadManager.AddRequireChunk(target, this);
				}
			}
		}
		
		
		internal Task<bool> IsAuthenticate(byte[] sharedKey)
		{
			return Task.Run(() =>
			{
				try
				{
					using var ms = new MemoryStream();

					var ascii = Encoding.ASCII.GetBytes("");
					ms.Write(ascii, 0, ascii.Length);
					ms.Write(sharedKey, 0, 16);
					ms.Write(Bukkit.MinecraftServer._cryptography.PublicKey);

					var serverHash = SelfCryptography.JavaHexDigest(ms.ToArray());
					var address =
						$"https://sessionserver.mojang.com/session/minecraft/hasJoined?username={Name}&serverId={serverHash}";
					using var wc = new WebClient();
					var auth = wc.DownloadString(address);
					if (auth.Length <= 10) return false;

					var json = JsonBuilder.Parse(auth);
					Profile.Name = json.Get<string>("name");
					var id = json.Get<string>("id");
					Profile.Uuid = new Uuid(id);
					
					Bukkit.ConsoleSender.SendMessage($"§eUser {Name} authenticated with UUID {id}");
					return true;
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(e);
				}

				return false;
			});
		}
	}
}