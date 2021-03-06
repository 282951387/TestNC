using System;
using System.Collections.Generic;

namespace ParadoxNotion
{

    ///A simple weak reference list
    public class WeakReferenceList<T> where T : class
    {
        private List<WeakReference<T>> list;

        public int Count => list.Count;

        public WeakReferenceList()
        {
            list = new List<WeakReference<T>>();
        }

        public WeakReferenceList(int capacity)
        {
            list = new List<WeakReference<T>>(capacity);
        }

        public T this[int i]
        {
            get
            {
                list[i].TryGetTarget(out T reference);
                return reference;
            }
            set
            {
                list[i].SetTarget(value);
            }
        }

        public void Add(T item)
        {
            list.Add(new WeakReference<T>(item));
        }

        public void Remove(T item)
        {
            for (int i = list.Count; i-- > 0;)
            {
                WeakReference<T> element = list[i];
                if (element.TryGetTarget(out T reference) && ReferenceEquals(reference, item))
                {
                    list.Remove(element);
                }
            }
        }

        public bool Contains(T item, out int index)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].TryGetTarget(out T target) && ReferenceEquals(target, item))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public void Clear()
        {
            list.Clear();
        }

        public List<T> ToReferenceList()
        {
            List<T> result = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                WeakReference<T> element = list[i];
                if (element.TryGetTarget(out T reference))
                {
                    result.Add(reference);
                }
            }
            return result;
        }

        public static implicit operator WeakReferenceList<T>(List<T> value)
        {
            WeakReferenceList<T> result = new WeakReferenceList<T>(value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                result.Add(value[i]);
            }
            return result;
        }
    }
}