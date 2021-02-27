using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Bingo
{
    public class World
    {
        private static ushort nextWorldId = 0;
        internal static World[] Worlds = new World[1];

        private ushort Id { get; }

        public uint NextEntityId { get; private set; }

        private readonly HashSet<Type> componentTypes = new();
        public ComponentTypesEnumerator ComponentTypes => new(componentTypes);

        public readonly struct ComponentTypesEnumerator
        {
            private readonly HashSet<Type> componentTypes;

            public ComponentTypesEnumerator(HashSet<Type> componentTypes) => this.componentTypes = componentTypes;

            public HashSet<Type>.Enumerator GetEnumerator() => componentTypes.GetEnumerator();
        }

        public World(uint nextId = 1)
        {
            if (nextId <= 0) throw new ArgumentOutOfRangeException(nameof(nextId));

            NextEntityId = nextId;
            Id = nextWorldId++;

            if (Worlds.Length == Id)
            {
                Array.Resize(ref Worlds, Worlds.Length * 2);
            }
            Worlds[Id] = this;
        }

        private ComponentStorage<T> GetStorage<T>() where T : struct, IComponent
        {
            return ComponentStorages<T>.Get(Id);
        }

        public Entity CreateEntity()
        {
            return new Entity(NextEntityId++, Id);
        }

        public Entity CreateEntity(uint id)
        {
            return new Entity(id, Id);
        }

        public ReadOnlySpan<Entity> EntitiesWith<T>() where T : struct, IComponent
        {
            var storage = GetStorage<T>();
            return storage.IndexToEntity.AsSpan(1, storage.Count - 1); //The 0th index is invalid, so go from 1
        }

        internal void Add<T>(Entity entity, T component) where T : struct, IComponent
        {
            GetStorage<T>().Add(entity, component);
            componentTypes.Add(typeof(T));
        }

        internal void Remove<T>(Entity entity) where T : struct, IComponent
        {
            GetStorage<T>().Remove(entity);
        }

        internal ref T Get<T>(Entity entity) where T : struct, IComponent
        {
            return ref GetStorage<T>().Get(entity);
        }

        internal bool Has<T>(Entity entity) where T : struct, IComponent
        {
            return GetStorage<T>().Has(entity);
        }

        #region Storage
        private static class ComponentStorages<T> where T : struct, IComponent
        {
            private static ComponentStorage<T>[] worldToStorage = new ComponentStorage<T>[1];

            public static ComponentStorage<T> Get(ushort worldId)
            {
                if (worldToStorage.Length <= worldId)
                {
                    Array.Resize(ref worldToStorage, nextWorldId * 2);
                }

                if (worldToStorage[worldId] == null)
                {
                    worldToStorage[worldId] = new ComponentStorage<T>();
                }

                return worldToStorage[worldId];
            }
        }

        private class ComponentStorage<T> where T : struct, IComponent
        {
            protected static readonly uint pageSize = (uint)(Environment.SystemPageSize / Marshal.SizeOf<Entity>());

            //Start from 1, since 0 is not a valid index.
            public int Count = 1;
            //A dense array of our actual components tracked by Count.
            public T[] Dense = new T[pageSize];
            //A dense array that ties our components in Array to their Entities tracked by Count.
            public Entity[] IndexToEntity = new Entity[pageSize];
            //A sparse array of buckets that ties entities to components.
            private int[][] entityToIndex = new int[pageSize][];

            public void Add(Entity entity, T component)
            {
                if (entity.Has<T>())
                {
                    throw new InvalidOperationException($"{entity.Id} already has {nameof(T)}.");
                }

                if (Count == Dense.Length)
                {
                    Array.Resize(ref Dense, Dense.Length * 2);
                    Array.Resize(ref IndexToEntity, Dense.Length * 2);
                }
                Dense[Count] = component;
                IndexToEntity[Count] = entity;

                if (entity.Id >= entityToIndex.Length)
                {
                    Array.Resize(ref entityToIndex, (int)(entity.Id * 3 / 2));
                }
                SetEntityToIndex(entity, Count);
                Count++;
            }

            public void Remove(Entity entity)
            {
                entity.Get<T>().OnDestroy(entity);

                int index = GetEntityToIndex(entity);
                Count--;
                Dense[index] = Dense[Count];
                Dense[Count] = default;
                var otherEntity = IndexToEntity[Count];
                SetEntityToIndex(otherEntity, index);
                IndexToEntity[index] = otherEntity;
                IndexToEntity[Count] = default;
                SetEntityToIndex(entity, 0);
            }

            public ref T Get(Entity entity)
            {
                return ref Dense[GetEntityToIndex(entity)];
            }

            public bool Has(Entity entity)
            {
                int index = GetEntityToIndex(entity);
                return index != 0 && IndexToEntity[index].Id != 0;
            }

            public int GetEntityToIndex(Entity entity)
            {
                uint bucketindex = entity.Id / pageSize;
                if (bucketindex >= entityToIndex.Length)
                    return 0;

                return entityToIndex[bucketindex]?[entity.Id % pageSize] ?? 0;
            }

            public void SetEntityToIndex(Entity entity, int index)
            {
                uint bucketindex = entity.Id / pageSize;
                if (bucketindex >= entityToIndex.Length)
                {
                    Array.Resize(ref entityToIndex, (int)(bucketindex * 2));
                }

                var buffer = entityToIndex[bucketindex];
                if (buffer == null)
                {
                    buffer = entityToIndex[bucketindex] = new int[pageSize];
                }
                buffer[entity.Id % pageSize] = index;
            }
        }
        #endregion
    }
}
