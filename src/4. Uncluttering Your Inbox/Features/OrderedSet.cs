// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// The ordered set.
    /// </summary>
    /// <typeparam name="T">The type of the collection </typeparam>
    public class OrderedSet<T> : ICollection<T>
    {
        /// <summary>
        /// The dictionary
        /// </summary>
        private readonly IDictionary<T, LinkedListNode<T>> dictionary;
        
        /// <summary>
        /// The linked list
        /// </summary>
        private readonly LinkedList<T> linkedList;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        public OrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        public OrderedSet(IEnumerable<T> items)
            : this(EqualityComparer<T>.Default)
        {
            if (items == null)
            {
                return;
            }

            foreach (T item in items)
            {
                this.Add(item);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        public OrderedSet(IEqualityComparer<T> comparer)
        {
            this.dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            this.linkedList = new LinkedList<T>();
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count
        {
            get { return this.dictionary.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        public virtual bool IsReadOnly
        {
            get { return this.dictionary.IsReadOnly; }
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if the item was added, otherwise false.</returns>
        public bool Add(T item)
        {
            if (this.dictionary.ContainsKey(item))
            {
                return false;
            }

            LinkedListNode<T> node = this.linkedList.AddLast(item);
            this.dictionary.Add(item, node);
            return true;
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            this.linkedList.Clear();
            this.dictionary.Clear();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        public bool Remove(T item)
        {
            LinkedListNode<T> node;
            bool found = this.dictionary.TryGetValue(item, out node);
            if (!found)
            {
                return false;
            }

            this.dictionary.Remove(item);
            this.linkedList.Remove(node);
            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return this.linkedList.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return this.dictionary.ContainsKey(item);
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.linkedList.CopyTo(array, arrayIndex);
        }
    }
}