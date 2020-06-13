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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

using OpenCollar.Extensions.Validation;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     A class that can hold a fixed set of values with an efficient key-based means of retrieving the values. This
    ///     class cannot be inherited.
    /// </summary>
    /// <typeparam name="TKey">
    ///     The type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///     The type of the value.
    /// </typeparam>
    /// <seealso cref="IFrozenLookup{TKey,TValue}" />
    /// /// /// ///
    /// <remarks>
    ///     This class is quicker and more memory efficient than a standard dictionary, at the cost of not being able to
    ///     add new items after construction.
    /// </remarks>
    [DebuggerDisplay("FrozenLookup: {ToString()}")]
    public sealed class FrozenLookup<TKey, TValue> : IFrozenLookup<TKey, TValue>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        /// <summary>
        ///     A count of the number of items in the dictionary.
        /// </summary>
        private readonly int _count;

        /// <summary>
        ///     The keys identifying the values held in this dictionary.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly TKey[] _keys
            ; // TODO: Is there some way we can recycle these?  The chances are there are only a few dozen distinct instances of these keys in use in the whole app.

        /// <summary>
        ///     The values held in this dictionary.
        /// </summary>
        [JetBrains.Annotations.CanBeNull]
        private readonly TValue[] _values;

        /// <summary>
        ///     The value of the to string method.
        /// </summary>
        [JetBrains.Annotations.CanBeNull]
        private string _toStringValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrozenLookup{TKey,TValue}" /> class.
        /// </summary>
        /// <param name="values">
        ///     The values held in this dictionary.
        /// </param>
        /// <param name="getKey">
        ///     A function that will return the key for a given value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <paramref name="getKey" /> function returned <see langword="null" /> for a value.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     Two or more values correspond to the same key.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     The <paramref name="values" /> argument was <see langword="null" />.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     The <paramref name="values" /> argument was empty.
        /// </exception>
        public FrozenLookup([JetBrains.Annotations.NotNull] TValue[] values, [JetBrains.Annotations.NotNull] Func<TValue, TKey> getKey)
        {
            values.Validate(nameof(values), ObjectIs.NotNull);

            // TODO: Add empty array validation
            _count = values.Length;

            // Special case to optimize performance of zero item loads.
            if(_count <= 0)
            {
                _keys = new TKey[0];
                return;
            }

            _keys = new TKey[_count];
            _values = new TValue[_count];

            // Special case to optimize performance of single item loads.
            if(_count == 1)
            {
                _values[0] = values[0];
                _keys[0] = getKey(values[0]);
                return;
            }

            // Special case to optimize performance of two item loads.
            if(_count == 2)
            {
                var key0 = GetValidKey(getKey, values[0]);
                var key1 = GetValidKey(getKey, values[1]);

                var c = key0.CompareTo(key1);
                if(c == 0)
                    throw new ArgumentOutOfRangeException(nameof(values), values,
                                                          string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.DuplicateKey, key0));
                if(c < 0)
                {
                    _keys[0] = key0;
                    _values[0] = values[0];
                    _keys[1] = key1;
                    _values[1] = values[1];
                }
                else
                {
                    _keys[0] = key1;
                    _values[0] = values[1];
                    _keys[1] = key0;
                    _values[1] = values[0];
                }

                return;
            }

            // General case to handle loads with more than two items
            var temp = values.Select(v => new Tuple<TKey, TValue>(GetValidKey(getKey, v), v)).OrderBy(t =>
            {
                Debug.Assert(t != null, "t != null");
                return t.Item1;
            });
            var n = 0;
            var previous = default(TKey);
            foreach(var t in temp)
            {
                Debug.Assert(t != null, "t != null");

                if(n > 0)
                {
                    Debug.Assert(t.Item1 != null, "t.Item1 != null");
                    if(t.Item1.CompareTo(previous) == 0)
                        throw new ArgumentOutOfRangeException(nameof(values), values,
                                                              string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.DuplicateKey, previous));
                }
                previous = t.Item1;

                _keys[n] = previous;
                _values[n] = t.Item2;
                ++n;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrozenLookup{TKey,TValue}" /> class.
        /// </summary>
        /// <param name="cloneFrom">
        ///     The dictionary from which to clone.
        /// </param>
        /// <param name="copyExistingValues">
        ///     If set to <see langword="true" /> the values from the existing dictionary are copied; otherwise the
        ///     values are left as default.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <paramref name="cloneFrom" /> argument was <see langword="null" />.
        /// </exception>
        public FrozenLookup([JetBrains.Annotations.NotNull] FrozenLookup<TKey, TValue> cloneFrom, bool copyExistingValues)
        {
            cloneFrom.Validate(nameof(cloneFrom), ObjectIs.NotNull);

            _count = cloneFrom._count;
            _keys = new TKey[_count];
            if(_count <= 0)
                return;

            Debug.Assert(cloneFrom._values != null, "cloneFrom._values != null");

            _values = new TValue[_count];

            for(var n = 0; n < _count; ++n)
            {
                _keys[n] = cloneFrom._keys[n];
                if(copyExistingValues)
                    _values[n] = cloneFrom._values[n];
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrozenLookup{TKey,TValue}" /> class.
        /// </summary>
        /// <param name="entries">
        ///     The an array of entries from which the key and value can be derived.
        /// </param>
        /// <param name="getKey">
        ///     A function that will return the key for a given entry.
        /// </param>
        /// <param name="getValue">
        ///     A function that will return the value for a given entry.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <paramref name="entries" /> argument was <see langword="null" />.
        /// </exception>
        public FrozenLookup([JetBrains.Annotations.NotNull] object[] entries, [JetBrains.Annotations.NotNull] Func<object, TKey> getKey,
                            [JetBrains.Annotations.NotNull] Func<object, TValue> getValue)
        {
            entries.Validate(nameof(entries), ObjectIs.NotNull);

            _count = entries.Length;

            if(_count <= 0)
            {
                _count = 0;
                _keys = new TKey[0];
                return;
            }

            _keys = new TKey[_count];
            _values = new TValue[_count];

            if(_count == 1)
            {
                _values[0] = getValue(entries[0]);
                _keys[0] = GetValidKey(getKey, entries[0]);
                return;
            }

            var temp = entries.Select(v => new Tuple<TKey, TValue>(GetValidKey(getKey, v), getValue(v))).OrderBy(t =>
            {
                Debug.Assert(t != null, "t != null");
                return t.Item1;
            });
            var n = 0;
            var previous = default(TKey);
            foreach(var t in temp)
            {
                Debug.Assert(t != null, "t != null");

                if(n > 0)
                {
                    Debug.Assert(t.Item1 != null, "t.Item1 != null");
                    if(t.Item1.CompareTo(previous) == 0)
                        throw new ArgumentOutOfRangeException(nameof(entries), entries,
                                                              string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.DuplicateKey, previous));
                }
                previous = t.Item1;

                _keys[n] = t.Item1;
                _values[n] = t.Item2;
                ++n;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FrozenLookup{TKey,TValue}" /> class.
        /// </summary>
        /// <param name="keys">
        ///     The keys to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <paramref name="keys" /> argument was <see langword="null" />.
        /// </exception>
        public FrozenLookup([JetBrains.Annotations.NotNull] TKey[] keys)
        {
            keys.Validate(nameof(keys), ObjectIs.NotNull);
            _count = keys.Length;
            _keys = keys;
            if(_count > 0)
                _values = new TValue[_count];
        }

        /// <summary>
        ///     Gets a count of the number of items in the dictionary.
        /// </summary>
        /// <value>
        ///     The number of items in the dictionary.
        /// </value>
        public int Count => _count;

        /// <summary>
        ///     Gets the keys identifying the values held in this dictionary.
        /// </summary>
        /// <value>
        ///     The keys identifying the values held in this dictionary.
        /// </value>
        public IEnumerable<TKey> Keys => _keys;

        /// <summary>
        ///     Gets the values held in this dictionary.
        /// </summary>
        /// <value>
        ///     The values held in this dictionary.
        /// </value>
        public IEnumerable<TValue> Values
        {
            get
            {
                if(_values == null)
                    return Enumerable.Empty<TValue>();

                return _values;
            }
        }

        /// <summary>
        ///     Gets the values held in this dictionary.
        /// </summary>
        /// <value>
        ///     The values held in this dictionary.
        /// </value>
        public IEnumerable<TValue> ValuesSnapshot
        {
            get
            {
                if(_values == null)
                    return Enumerable.Empty<TValue>();

                return (TValue[])_values.Clone();
            }
        }

        /// <summary>
        ///     Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <returns>
        ///     The value requested.
        /// </returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        ///     No item with the key specified could be found in the lookup.
        /// </exception>
        public TValue this[TKey key]
        {
            get
            {
                var n = GetEntryIndex(key);
                if(n >= 0)
                    return _values[n];

                throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.ItemNotFound,
                                                             key));
            }

            set
            {
                var n = GetEntryIndex(key);
                if(n >= 0)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    _values[n] = value;
                    return;
                }

                throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.ItemNotFound,
                                                             key));
            }
        }

        /// <summary>
        ///     Implements the inequality operator.
        /// </summary>
        /// <param name="left">
        ///     The left operand.
        /// </param>
        /// <param name="right">
        ///     The right operand.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        public static bool operator !=([JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> left,
                                       [JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///     Implements the less-than operator.
        /// </summary>
        /// <param name="left">
        ///     The left operand.
        /// </param>
        /// <param name="right">
        ///     The right operand.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        public static bool operator <([JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> left,
                                      [JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> right)
        {
            return CompareTo(left, right) < 0;
        }

        /// <summary>
        ///     Implements the less-than-or-equals operator.
        /// </summary>
        /// <param name="left">
        ///     The left operand.
        /// </param>
        /// <param name="right">
        ///     The right operand.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        public static bool operator <=([JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> left,
                                       [JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> right)
        {
            return CompareTo(left, right) <= 0;
        }

        /// <summary>
        ///     Implements the equality operator.
        /// </summary>
        /// <param name="left">
        ///     The left operand.
        /// </param>
        /// <param name="right">
        ///     The right operand.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        public static bool operator ==([JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> left,
                                       [JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///     Implements the greater-than operator.
        /// </summary>
        /// <param name="left">
        ///     The left operand.
        /// </param>
        /// <param name="right">
        ///     The right operand.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        public static bool operator >([JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> left,
                                      [JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> right)
        {
            return CompareTo(left, right) > 0;
        }

        /// <summary>
        ///     Implements the greater-than-or-equals operator.
        /// </summary>
        /// <param name="left">
        ///     The left operand.
        /// </param>
        /// <param name="right">
        ///     The right operand.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        public static bool operator >=([JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> left,
                                       [JetBrains.Annotations.CanBeNull] FrozenLookup<TKey, TValue> right)
        {
            return CompareTo(left, right) >= 0;
        }

        /// <summary>
        ///     Returns a clone of the current dictionary.
        /// </summary>
        /// <returns>
        ///     A clone of the dictionary given, including its existing values.
        /// </returns>
        public IFrozenLookup<TKey, TValue> Clone()
        {
            return new FrozenLookup<TKey, TValue>(this, true);
        }

        /// <summary>
        ///     Returns a clone of the current dictionary.
        /// </summary>
        /// <returns>
        ///     A clone of the dictionary given.
        /// </returns>
        /// <param name="copyExistingValues">
        ///     If set to <see langword="true" /> the values from the existing dictionary are copied; otherwise the
        ///     values are left as default.
        /// </param>
        public IFrozenLookup<TKey, TValue> Clone(bool copyExistingValues)
        {
            return new FrozenLookup<TKey, TValue>(this, copyExistingValues);
        }

        /// <summary>
        ///     Returns a clone of the dictionary given, with no values set and using <typeparamref name="TNew" /> as
        ///     the value type.
        /// </summary>
        /// <returns>
        ///     A clone of the current dictionary given.
        /// </returns>
        public IFrozenLookup<TKey, TNew> Clone<TNew>()
        {
            return new FrozenLookup<TKey, TNew>(_keys);
        }

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an integer that indicates
        ///     whether the current instance precedes, follows, or occurs in the same position in the sort order as the
        ///     right object.
        /// </summary>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has these
        ///     meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order.
        ///     Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater
        ///     than zero This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        /// <param name="obj">
        ///     An object to compare with this instance.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="obj" /> is not the same type as this instance.
        /// </exception>
        public int CompareTo(object obj)
        {
            if(ReferenceEquals(obj, null))
                return 1;

            var other = obj as FrozenLookup<TKey, TValue>;
            if(ReferenceEquals(other, null))
                throw new ArgumentException(Resources.Exceptions.ObjectIsNotTheSameTypeAsThisInstance);

            return CompareTo(this, other);
        }

        /// <summary>
        ///     Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has the following
        ///     meanings: Value Meaning Less than zero This object is less than the <paramref name="other" />
        ///     parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is
        ///     greater than <paramref name="other" />.
        /// </returns>
        /// <param name="other">
        ///     An object to compare with this object.
        /// </param>
        public int CompareTo(IFrozenLookup<TKey, TValue> other)
        {
            return CompareTo(this, other);
        }

        /// <summary>
        ///     Determines whether an item with the specified key exists in this dictionary.
        /// </summary>
        /// <param name="key">
        ///     The key for which to look.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if the specified key is found; otherwise, <see langword="false" />.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            if(ReferenceEquals(key, null))
                return false;

            return GetEntryIndex(key) >= 0;
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter;
        ///     otherwise, <see langword="false" />.
        /// </returns>
        /// <param name="other">
        ///     An object to compare with this object.
        /// </param>
        public bool Equals(IFrozenLookup<TKey, TValue> other)
        {
            return CompareTo(this, other) == 0;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" /> is equal to the current <see cref="System.Object" />.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if the specified <see cref="System.Object" /> is equal to the current
        ///     <see cref="System.Object" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <param name="obj">
        ///     The <see cref="System.Object" /> to compare with the current <see cref="System.Object" />.
        /// </param>
        public override bool Equals(object obj)
        {
            var other = obj as FrozenLookup<TKey, TValue>;
            if(ReferenceEquals(other, null))
                return false;

            return Equals(other);
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        ///     A hash code for the current <see cref="System.Object" />.
        /// </returns>
        public override int GetHashCode()
        {
            if(_count <= 0)
                return 0;

            Debug.Assert(_values != null, "_values != null");

            var hashcode = 1;
            unchecked
            {
                for(var n = 0; n < _count; ++n)

                // ReSharper disable once PossibleNullReferenceException
                {
                    hashcode = hashcode + (_keys[n].GetHashCode() * 37) +
                               (ReferenceEquals(_values[n], null) ? 0 : _values[n].GetHashCode() * 79);
                }
            }

            return hashcode;
        }

        /// <summary>
        ///     Returns the key held at the index given.
        /// </summary>
        /// <param name="index">
        ///     The index at which to take the key.
        /// </param>
        /// <returns>
        ///     The key at the index specified.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> must be value greater than or equal to zero and less than the value returned
        ///     by the <see cref="Count" /> property.
        /// </exception>
        public TKey KeyAt(int index)
        {
            if((index < 0) || (index >= _count))
                throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Exceptions.IndexOutOfRange);

            var key = _keys[index];
            Debug.Assert(key != null, "key != null");
            return key;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents the current <see cref="System.Object" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents the current <see cref="System.Object" />.
        /// </returns>
        public override string ToString()
        {
            if(!ReferenceEquals(_toStringValue, null))
                return _toStringValue;

            // _keys is guaranteed not to be null and using it saves us the need to create a new object to just act as a
            // lock token that will be used only once or twice.
            lock(_keys)
            {
                if(!ReferenceEquals(_toStringValue, null))
                    return _toStringValue;

                _toStringValue = GetToString();

                return _toStringValue;
            }
        }

        /// <summary>
        ///     Tries the get value specified by the key given.
        /// </summary>
        /// <param name="key">
        ///     The key identifying the value to find.
        /// </param>
        /// <param name="value">
        ///     An argument in which to return the value.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if the value can be found, <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// </exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var n = GetEntryIndex(key);
            if(n >= 0)
            {
                // ReSharper disable once PossibleNullReferenceException
                value = _values[n];
                return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        ///     Returns the value held at the index given.
        /// </summary>
        /// <param name="index">
        ///     The index at which to take the Value.
        /// </param>
        /// <returns>
        ///     The value at the index specified.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> must be value greater than or equal to zero and less than the value returned
        ///     by the <see cref="Count" /> property.
        /// </exception>
        public TValue ValueAt(int index)
        {
            if((index < 0) || (index >= _count))
                throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Exceptions.IndexOutOfRange);

            Debug.Assert(_values != null, "_values != null");

            return _values[index];
        }

        /// <summary>
        ///     Compares the <paramref name="left" /> with <paramref name="right" /> of the same type and returns an
        ///     integer that indicates whether the <paramref name="left" /> precedes, follows, or occurs in the same
        ///     position in the sort order as the other object.
        /// </summary>
        /// <param name="left">
        ///     The first object to compare.
        /// </param>
        /// <param name="right">
        ///     An object to compare with <paramref name="left" />.
        /// </param>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has these
        ///     meanings: Value Meaning Less than zero <paramref name="left" /> precedes <paramref name="right" /> in
        ///     the sort order. Zero This instance occurs in the same position in the sort order as
        ///     <paramref name="right" />. Greater than zero This instance follows <paramref name="right" /> in the sort order.
        /// </returns>
        private static int CompareTo(IFrozenLookup<TKey, TValue> left, IFrozenLookup<TKey, TValue> right)
        {
            if(ReferenceEquals(left, right))
                return 0;

            if(ReferenceEquals(left, null))
                return -1;

            if(ReferenceEquals(right, null))
                return +1;

            var c = left.Count - right.Count;
            if(c != 0)
                return c;

            // The keys will be sorted, so we can compare keys and values in a single pass.
            for(var n = 0; n < left.Count; ++n)
            {
                // ReSharper disable once PossibleNullReferenceException
                c = left.KeyAt(n).CompareTo(right.KeyAt(n));
                if(c != 0)
                    return c;

                c = OpenCollar.Extensions.Compare.CompareAny(left.ValueAt(n), right.ValueAt(n));
                if(c != 0)
                    return c;
            }

            return 0;
        }

        /// <summary>
        ///     Gets the key for the value given using the function specified and validates the key before returning it.
        /// </summary>
        /// <param name="getKey">
        ///     A function that will return the key for a given value.
        /// </param>
        /// <param name="value">
        ///     The value to which the key applies.
        /// </param>
        /// <returns>
        ///     The validated key.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     The key generated for the value was <see langword="null" />.
        /// </exception>
        [JetBrains.Annotations.NotNull]
        private static TKey GetValidKey([JetBrains.Annotations.NotNull] Func<TValue, TKey> getKey, [JetBrains.Annotations.CanBeNull] TValue value)
        {
            var key = getKey(value);

            if(ReferenceEquals(key, null))
                throw new ArgumentOutOfRangeException(nameof(getKey), null,
                                                      string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.NullKeyGenerated, value));

            return key;
        }

        /// <summary>
        ///     Gets the key for the value given using the function specified and validates the key before returning it.
        /// </summary>
        /// <param name="getKey">
        ///     A function that will return the key for a given value.
        /// </param>
        /// <param name="value">
        ///     The value to which the key applies.
        /// </param>
        /// <returns>
        ///     The validated key.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     The key generated for the value was <see langword="null" />.
        /// </exception>
        [JetBrains.Annotations.NotNull]
        private static TKey GetValidKey([JetBrains.Annotations.NotNull] Func<object, TKey> getKey, [JetBrains.Annotations.CanBeNull] object value)
        {
            var key = getKey(value);

            if(ReferenceEquals(key, null))
                throw new ArgumentOutOfRangeException(nameof(getKey), null,
                                                      string.Format(CultureInfo.InvariantCulture, Resources.Exceptions.NullKeyGenerated, value));

            return key;
        }

        /// <summary>
        ///     Gets the index of the entry specified by the key given.
        /// </summary>
        /// <param name="key">
        ///     The key for which the index is required.
        /// </param>
        /// <returns>
        ///     The index of the entry requested, or -1 if it cannot be found.
        /// </returns>
        private int GetEntryIndex([JetBrains.Annotations.NotNull] TKey key)
        {
            // Optimizations for small dictionaries
            if(_count <= 2)
            {
                if(_count <= 0)
                    return -1;

                if(Equals(_keys[0], key))
                    return 0;

                if(_count <= 1)
                    return -1;

                if(Equals(_keys[1], key))
                    return 1;

                return -1;
            }

            // More than two items.
            var low = 0;
            var high = _count - 1;
            do
            {
                var mid = low + ((high - low) / 2);

                var c = key.CompareTo(_keys[mid]);
                if(c == 0)
                    return mid;

                if(c > 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            } while((low <= high) && (high >= 0));

            return -1;
        }

        /// <summary>
        ///     Gets a string representing the contents of this dictionary.
        /// </summary>
        /// <returns>
        ///     A string representing the contents of this dictionary.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        private string GetToString()
        {
            if(_count <= 0)
                return Resources.Captions.Empty;

            var builder = new StringBuilder();

            Debug.Assert(_values != null, "_values != null");

            for(var n = 0; n < _count; ++n)
            {
                if(n > 0)
                    builder.Append(';');

                builder.Append(_keys[n]);
                builder.Append('=');

                if(ReferenceEquals(_values[n], null))
                    builder.Append(Resources.Captions.Null);
                else
                    builder.Append(_values[n]);
            }

            return builder.ToString();
        }
    }
}