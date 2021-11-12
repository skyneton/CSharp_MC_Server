﻿using System;
using NiaBukkit.API.Util;

namespace NiaBukkit.API.Entities
{
    public class Entity
    {
        public readonly int EntityId;
        public Location Location { get; internal set; }
        public World.World World => Location.World;
        public bool IsOnGround { get; internal set; }

        public readonly Uuid Uuid;

        public Entity(World.World world) : this(Uuid.RandomUuid(), world, 0, 0, 0) { }
        public Entity(Uuid uuid, World.World world) : this(uuid, world, 0, 0, 0) { }

        public Entity(Uuid uuid, World.World world, double x, double y, double z)
        {
            Uuid = uuid;
            Location = new Location(world, 0, 5, 0);
            Location.Changeable = false;
            EntityId = GenerateEntityId();
        }

        public void SetLocation(double x, double y, double z, float yaw, float pitch)
        {
            Location = new Location(Location.World, x, y, z, yaw, pitch);
        }

        private static int _currentEntityId = -1;

        public static int GenerateEntityId()
        {
            if (_currentEntityId == Int32.MaxValue)
                return _currentEntityId = 0;

            return ++_currentEntityId;
        }

        internal virtual void Update() { }
    }
}