using System;

namespace Bingo
{
    public readonly struct Entity : IEquatable<Entity>
    {
        private readonly ushort worldId;
        public World World => World.Worlds[worldId];

        public readonly uint Id;

        internal Entity(uint id, ushort world)
        {
            Id = id;
            worldId = world;
        }

        public bool Has<T>() where T : struct, IComponent
        {
            return World.Worlds[worldId].Has<T>(this);
        }

        public ref T Get<T>() where T : struct, IComponent
        {
            return ref World.Worlds[worldId].Get<T>(this);
        }

        public void Add<T>(T component) where T : struct, IComponent
        {
            World.Worlds[worldId].Add(this, component);
        }

        public void Remove<T>() where T : struct, IComponent
        {
            World.Worlds[worldId].Remove<T>(this);
        }

        #region Equality and overrides
        public static bool operator ==(Entity left, Entity right) => left.Id == right.Id;
        public static bool operator !=(Entity left, Entity right) => left.Id != right.Id;

        public override bool Equals(object? obj) => obj switch
        {
            Entity other => Equals(other),
            _ => false
        };
        public bool Equals(Entity other) => Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id.ToString();
        #endregion
    }
}
