using System;

namespace Bingo
{
    public struct EntityBuffer
    {
        private Entity[] entities;
        public Span<Entity> Entities => entities.AsSpan(0, Count);
        public int Count { get; private set; }

        public Entity this[int index] => entities[index];

        public EntityBuffer(int capacity)
        {
            entities = new Entity[capacity];
            Count = 0;
        }

        public void Add(Entity entity)
        {
            if (entities == null)
            {
                entities = new Entity[1];
            }
            else if (Count == entities.Length)
            {
                Array.Resize(ref entities, Count * 2);
            }
            entities[Count++] = entity;
        }

        public void Remove(Entity entity)
        {
            RemoveAt(Array.IndexOf(entities, entity, 0, Count));
        }

        public void RemoveAt(int index)
        {
            entities[index] = entities[--Count];
            entities[Count] = default;
        }

        public Span<Entity>.Enumerator GetEnumerator() => Entities.GetEnumerator();
    }
}
