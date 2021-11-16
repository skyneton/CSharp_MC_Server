﻿using System;
using System.Collections.Generic;
using NiaBukkit.API.Util;
using NiaBukkit.Network;

namespace NiaBukkit.API.World.Chunks
{
    public class ChunkSection
    {
        public const int Size = 16 * 16 * 16;
        private int[] _blocks = new int[Size];//Enumerable.Repeat<int>(-1, Size).ToArray();
        private readonly List<Material> _palette = new List<Material>();
        private readonly Dictionary<Material, int> _inversePalette = new Dictionary<Material, int>();

        private readonly NibbleArray _blockLight = new NibbleArray(Size);
        private NibbleArray _skyLight;

        public byte YPos { get; private set; }
        
        internal static int Index(int x, int y, int z) => y << 8 | z << 4 | x;

        public int PaletteSize => _palette.Count;

        public ChunkSection()
        {
            GetOrCreatePaletteIndex(Material.Air);
        }

        public ChunkSection(byte yPos) : this()
        {
            YPos = yPos;
        }

        public void SetBlockLight(int x, int y, int z, byte data)
        {
            _blockLight[x, y, z] = data;
        }

        public void SetSkyLight(int x, int y, int z, byte data)
        {
            if (_skyLight == null)
                _skyLight = new NibbleArray(Size);
            
            _skyLight[x, y, z] = data;
        }

        public Material GetPalette(int index)
        {
            return _palette[index];
        }
        public int GetOldPaletteData(int i) => GetPalette(i).GetOldId() << 4 | GetPalette(i).GetOldSubId();

        public int GetOrCreatePaletteIndex(Material block)
        {
            int index = _inversePalette.GetValueOrDefault(block, -1);
            if (index == -1)
            {
                index = _palette.Count;
                _palette.Add(block);
                _inversePalette.Add(block, index);
            }

            return index;
        }

        public void SetBlock(int x, int y, int z, Material block)
        {
            _blocks[Index(x, y, z)] = GetOrCreatePaletteIndex(block);
        }

        public Material GetBlock(int x, int y, int z)
        {
            return GetBlock(Index(x, y, z));
        }

        public Material GetBlock(int i)
        {
            int index = _blocks[i];
            if (index == -1)
                return Material.Air;
            
            return _palette[index];
        }

        public int GetBlockData(int i) => GetBlock(i).GetId();

        public int GetOldBlockData(int i) => GetBlock(i).GetOldId() << 4 | GetBlock(i).GetOldSubId();

        public int GetPaletteIndex(int i)
        {
            return _blocks[i];
        }

        public void WriteBlockLight(ByteBuf buf)
        {
            buf.Write(_blockLight.Data);
        }

        public void WriteSkyLight(ByteBuf buf)
        {
            buf.Write(_skyLight.Data);
        }

        public bool HasSkyLight() => _skyLight != null;
    }
}