﻿using NiaBukkit.API.NBT;
using NiaBukkit.API.Util;

namespace NiaBukkit.API.Blocks.Data
{
    public class BlockMinecartTrack : BlockData
    {
        public PropertyTrackPosition Shape { get; private set; }
        
        public BlockMinecartTrack(Material type) : base(type)
        {
        }

        internal override BlockData GetBlockData(NBTTagCompound properties)
        {
            return GetBlockData(new BlockMinecartTrack(Type), properties);
        }

        internal override BlockData GetBlockData(BlockData block, NBTTagCompound properties)
        {
            ((BlockMinecartTrack) block).Shape = properties.GetState(PropertyTrackPosition.NorthSouth);
            
            return base.GetBlockData(block, properties);
        }

        public override NBTTagCompound ToNBT()
        {
            var tag = base.ToNBT();
            var properties = tag.GetOrCreateCompound("Properties");
            properties.Set("shape", new NBTTagString(Shape.ToString().Name2Minecraft()));
            
            return tag;
        }

        public static bool operator ==(BlockMinecartTrack o1, BlockData o2)
        {
            if (o1 is null || o2 is null) return o1 is null && o2 is null;
            if (o2 is not BlockMinecartTrack o) return false;
            return o1.Shape == o.Shape && (BlockData) o1 == o;
        }

        public static bool operator !=(BlockMinecartTrack o1, BlockData o2) => !(o1 == o2);

        public override bool Equals(object obj)
        {
            if (obj is not BlockMinecartTrack data) return false;
            return this == data;
        }

        public override int GetFlatId() => base.GetFlatId() | Shape.GetMeta();
    }
}