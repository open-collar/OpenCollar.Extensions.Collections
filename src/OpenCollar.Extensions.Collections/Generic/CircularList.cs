/*
 * This file is part of OpenCollar.Extensions.Collections.
 *
 * OpenCollar.Extensions.Collections is free software: you can redistribute it
 * and/or modify it under the terms of the GNU General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version.
 *
 * OpenCollar.Extensions.Collections is distributed in the hope that it will be
 * useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
 * License for more details.
 *
 * You should have received a copy of the GNU General Public License along with
 * OpenCollar.Extensions.Collections.  If not, see <https://www.gnu.org/licenses/>.
 *
 * Copyright © 2019-2020 Jonathan Evans (jevans@open-collar.org.uk).
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using OpenCollar.Extensions.Validation;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     A fixed size list of type <typeparamref name="T" />. When an item is added it is placed in the next free
    ///     space, when no more free spaces are available the original items in the list are overwritten. The oldest
    ///     item is always the first item in the list.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of value the list will hold.
    /// </typeparam>
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification =
        "Usage and functionality is much more similar to a list.")]
    public class CircularList<T> : IList<T>
    {
        /// <summary>
        ///     The data across which we will iterate.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly T[] _data;

        /// <summary>
        ///     An access control token for the <see cref="_data" /> field.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly object _dataLock = new object();

        /// <summary>
        ///     The length of the list.
        /// </summary>
        private readonly int _maxLength;

        /// <summary>
        ///     The index of the oldest piece of data.
        /// </summary>
        private int _first;

        /// <summary>
        ///     A flag indicating that the next position has looped past the end of the list.
        /// </summary>
        private bool _looped;

        /// <summary>
        ///     The current insertion point.
        /// </summary>
        private int _next;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CircularList{T}" /> class.
        /// </summary>
        /// <param name="length">
        ///     The length of the list.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="length" /> is less than 1.
        /// </exception>
        public CircularList(int length)
        {
            length.Validate(nameof(length), 1, int.MaxValue);

            _maxLength = length;
            _data = new T[_maxLength];
            Clear();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CircularList{T}" /> class and loads the data supplied.
        /// </summary>
        /// <param name="length">
        ///     The length of the list.
        /// </param>
        /// <param name="values">
        ///     The values with which to load the list. The values are loaded backwards (e.g. the last first) to ensure
        ///     that the order is maintained and the oldest (i.e. last) items are dropped of they exceed the length of
        ///     the list.
        /// </param>
        public CircularList(int length, [JetBrains.Annotations.NotNull] IEnumerable<T> values) : this(length)
        {
            Load(values);
        }

        /// <summary>
        ///     Gets the number of elements contained in the <see cref="System.Collections.Generic.ICollection{T}" />
        ///     that have actually been set; as opposed to <see cref="MaxLength" /> which is the maximum size of the
        ///     circular list. When a list has been filled these two values will be the same.
        /// </summary>
        /// <returns>
        ///     The number of elements contained in the <see cref="System.Collections.Generic.ICollection{T}" /> that
        ///     have actually been set.
        /// </returns>
        public int Count
        {
            get
            {
                if(_looped)
                    return (_maxLength - _first) + _next;

                return _next - _first;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="System.Collections.Generic.ICollection{T}" /> is read-only.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if the <see cref="System.Collections.Generic.ICollection{T}" /> is read-only;
        ///     otherwise, <see langword="false" />.
        /// </returns>
        public bool IsReadOnly => false;

        /// <summary>
        ///     Gets the maximum length of the list.
        /// </summary>
        public int MaxLength => _maxLength;

        /// <summary>
        ///     Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        ///     The element at the specified index.
        /// </returns>
        /// <param name="index">
        ///     The zero-based index of the element to get or set.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="System.Collections.Generic.IList{T}" />.
        /// </exception>
        [JetBrains.Annotations.CanBeNull]
        [IndexerName("Item")]
        public T this[int index]
        {
            get
            {
                index.Validate(nameof(index), 0, Count - 1);

                lock(_dataLock)
                {
                    return _data[TranslateIndex(index)];
                }
            }

            set
            {
                index.Validate(nameof(index), 0, Count - 1);

                lock(_dataLock)
                {
                    _data[TranslateIndex(index)] = value;
                }
            }
        }

        /// <summary>
        ///     Adds an item to the <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </param>
        /// <exception cref="System.NotSupportedException">
        ///     The <see cref="System.Collections.Generic.ICollection{T}" /> is read-only.
        /// </exception>
        public void Add([JetBrains.Annotations.NotNull] T item)
        {
            lock(_dataLock)
            {
                _data[_next++] = item;

                if(_next >= _maxLength)
                {
                    _next = 0;
                    _looped = true;
                }
                if(_looped)
                    _first = _next;
            }
        }

        /// <summary>
        ///     Removes all items from the <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </summary>
        /// <exception cref="System.NotSupportedException">
        ///     The <see cref="System.Collections.Generic.ICollection{T}" /> is read-only.
        /// </exception>
        public void Clear()
        {
            lock(_dataLock)
            {
                for(var n = 0; n < _maxLength; ++n)
                    _data[n] = default(T);
                _first = 0;
                _next = 0;
                _looped = false;
            }
        }

        /// <summary>
        ///     Determines whether the <see cref="System.Collections.Generic.ICollection{T}" /> contains a specific value.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="item" /> is found in the
        ///     <see cref="System.Collections.Generic.ICollection{T}" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <param name="item">
        ///     The object to locate in the <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </param>
        public bool Contains([JetBrains.Annotations.NotNull] T item)
        {
            lock(_dataLock)
            {
                for(var n = 0; n < _maxLength; ++n)
                {
                    if(_data[n].Equals(item))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Copies the elements of the <see cref="System.Collections.Generic.ICollection{T}" /> to an
        ///     <see cref="System.Array" />, starting at a particular <see cref="System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="System.Array" /> that is the destination of the elements copied from
        ///     <see cref="System.Collections.Generic.ICollection{T}" />. The <see cref="System.Array" /> must have
        ///     zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        ///     The zero-based index in <paramref name="array" /> at which copying begins.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="arrayIndex" /> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="array" /> is multidimensional. -or- <paramref name="arrayIndex" /> is equal to or
        ///     greater than the length of <paramref name="array" />. -or- The number of elements in the source
        ///     <see cref="System.Collections.Generic.ICollection{T}" /> is greater than the available space from
        ///     <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />. -or- Type
        ///     <paramtyperef name="T" /> cannot be cast automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        public void CopyTo([JetBrains.Annotations.NotNull] T[] array, int arrayIndex)
        {
            array.Validate(nameof(array), ObjectIs.NotNull);
            arrayIndex.Validate(nameof(arrayIndex), array.GetLowerBound(0), array.GetUpperBound(0));

            lock(_dataLock)
            {
                var count = Count;
                for(var n = 0; n < count; ++n)
                    array[arrayIndex + n] = _data[TranslateIndex(n)];
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.Collections.Generic.IEnumerator{T}" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>
        ///     1
        /// </filterpriority>
        [JetBrains.Annotations.NotNull]
        public IEnumerator<T> GetEnumerator()
        {
            return new CircularListEnumerator<T>(this);
        }

        /// <summary>
        ///     Determines the index of a specific item in the <see cref="System.Collections.Generic.IList{T}" />.
        /// </summary>
        /// <returns>
        ///     The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">
        ///     The object to locate in the <see cref="System.Collections.Generic.IList{T}" />.
        /// </param>
        public int IndexOf([JetBrains.Annotations.NotNull] T item)
        {
            lock(_dataLock)
            {
                for(var n = 0; n < _maxLength; ++n)
                {
                    var index = TranslateIndex(n);
                    if(_data[index].Equals(item))
                        return index;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Inserts an item to the <see cref="System.Collections.Generic.IList{T}" /> at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index at which <paramref name="item" /> should be inserted.
        /// </param>
        /// <param name="item">
        ///     The object to insert into the <see cref="System.Collections.Generic.IList{T}" />.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="System.Collections.Generic.IList{T}" />.
        /// </exception>
        /// <exception cref="System.NotSupportedException">
        ///     The <see cref="System.Collections.Generic.IList{T}" /> is read-only.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", Justification =
            "Validated by input parameter.")]
        public void Insert(int index, T item)
        {
            index.Validate(nameof(index), 0, Count - 2);

            lock(_dataLock)
            {
                for(var n = index + 1; n < _maxLength; ++n)
                    _data[TranslateIndex(n)] = _data[TranslateIndex(n - 1)];
                _data[TranslateIndex(index)] = item;
            }
        }

        /// <summary>
        ///     Loads circular array with the specified values.
        /// </summary>
        /// <param name="values">
        ///     The values with which to load the list. The values are loaded backwards (e.g. the last first) to ensure
        ///     that the order is maintained and the oldest (i.e. last) items are dropped of they exceed the length of
        ///     the list.
        /// </param>
        public void Load([JetBrains.Annotations.NotNull] IEnumerable<T> values)
        {
            values.Validate(nameof(values), ObjectIs.NotNull);
            lock(_dataLock)
            {
                Clear();

                foreach(var value in values.Reverse())
                    Add(value);
            }
        }

        /// <summary>
        ///     Removes the first occurrence of a specific object from the <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="item" /> was successfully removed from the
        ///     <see cref="System.Collections.Generic.ICollection{T}" />; otherwise, <see langword="false" />. This
        ///     method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </returns>
        /// <param name="item">
        ///     The object to remove from the <see cref="System.Collections.Generic.ICollection{T}" />.
        /// </param>
        /// <exception cref="System.NotSupportedException">
        ///     The <see cref="System.Collections.Generic.ICollection{T}" /> is read-only.
        /// </exception>
        public bool Remove([JetBrains.Annotations.NotNull] T item)
        {
            lock(_dataLock)
            {
                var last = Count;
                for(var n = 0; n < last; ++n)
                {
                    var index = TranslateIndex(n);
                    if(_data[index].Equals(item))
                    {
                        RemoveAt(n);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Removes the <see cref="System.Collections.Generic.IList{T}" /> item at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index of the item to remove.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="System.Collections.Generic.IList{T}" />.
        /// </exception>
        /// <exception cref="System.NotSupportedException">
        ///     The <see cref="System.Collections.Generic.IList{T}" /> is read-only.
        /// </exception>
        public void RemoveAt(int index)
        {
            index.Validate(nameof(index), 0, Count - 1);

            lock(_dataLock)
            {
                var last = Count - 1;
                for(var n = index; n < (_maxLength - 1); ++n)
                    _data[TranslateIndex(n)] = _data[TranslateIndex(n + 1)];

                _data[TranslateIndex(last)] = default(T);

                --_next;
                if(_next < 0)
                {
                    _next = _maxLength - 1;
                    _looped = false;
                }
            }
        }

        /// <summary>
        ///     Copies the contents of the circular list to an array and returns it.
        /// </summary>
        /// <returns>
        ///     An array containing all the items in the list, in order.
        /// </returns>
        public T[] ToArray()
        {
            lock(_dataLock)
            {
                var array = new T[Count];
                CopyTo(array, 0);
                return array;
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>
        ///     2
        /// </filterpriority>
        [JetBrains.Annotations.NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Translates the index from the outside appearance of a zero-based linear list to a position in the list
        ///     relative to the first position.
        /// </summary>
        /// <param name="index">
        ///     The index to translate.
        /// </param>
        /// <returns>
        ///     A new index that takes into account the first position. This might be out of range if the original value
        ///     was out of range.
        /// </returns>
        private int TranslateIndex(int index)
        {
            var result = _first + index;
            if(result >= _maxLength)
                return result - _maxLength;
            return result;
        }
    }
}