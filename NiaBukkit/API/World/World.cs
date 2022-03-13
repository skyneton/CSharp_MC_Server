﻿using System;
using System.Collections.Concurrent;
using NiaBukkit.API.Blocks;
using NiaBukkit.API.Config;
using NiaBukkit.API.Entities;
using NiaBukkit.API.Util;
using NiaBukkit.API.World.Chunks;
using NiaBukkit.API.World.Provider;

namespace NiaBukkit.API.World
{
    public class World
    {
        // WorldProvider
        internal Dimention Dimension = Dimention.OverWorld;
        public Difficulty Difficulty = ServerProperties.Difficulty;
        public WorldType WorldType;

        public long Seed = new Random().Next(-10, 10);

        public readonly ConcurrentBag<Player> Players = new ConcurrentBag<Player>();

        private readonly WorldProvider _provider;
        public bool IsHardCore { get; internal set; } = ServerProperties.Hardcore;

        private readonly ConcurrentDictionary<ChunkCoord, Chunk> _loadedChunks = new ConcurrentDictionary<ChunkCoord, Chunk>();

        public readonly ConcurrentBag<Entity> Entities = new ConcurrentBag<Entity>();

        public readonly string Name;

        public Location WorldSpawn { get; private set; }

        public World(string name)
        {
            Name = name;
            WorldType = WorldType.Flat;
            _provider = new WorldProviderFlat(this);
            
            WorldSpawn = new Location(this, 0, 5, 0);
        }

        internal Chunk GetChunk(int x, int z)
        {
            _loadedChunks.TryGetValue(new ChunkCoord(this, x, z), out var chunk);
            if (chunk != null)
                return chunk;

            // return _provider.GetChunk(x, z);
            return ChunkRegionManager.GetChunk(this, x, z) ?? _provider.GetChunk(x, z);
        }

        internal void AddChunk(Chunk chunk)
        {
            if (chunk.Coord.World != this) return;
            _loadedChunks.GetOrAdd(chunk.Coord, chunk);
        }

        internal Chunk RemoveChunk(int x, int z)
        {
            _loadedChunks.TryRemove(new ChunkCoord(this, x, z), out var chunk);

            return chunk;
        }

        public BlockData GetBlock(int x, int y, int z)
        {
            return GetChunk(x >> 4, z >> 4).GetBlock(x % 16, y, z % 16);
        }

        public byte GetLightLevel(int x, int y, int z)
        {
            return GetChunk(x >> 4, z >> 4).GetLightLevel(x % 16, y, z % 16);
        }
    }
}