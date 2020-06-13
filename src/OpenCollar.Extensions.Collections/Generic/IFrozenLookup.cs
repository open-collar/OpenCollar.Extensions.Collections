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

using JetBrains.Annotations;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     The interface implemented by the simple data structure in which primary keys and similar read-only key/value
    ///     pairs are stored.
    /// </summary>
    /// <typeparam name="TKey">
    ///     The type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///     The type of the value.
    /// </typeparam>
    /// <seealso cref="System.IComparable{T}" />
    /// /// /// ///
    /// <seealso cref="System.IEquatable{T}" />
    public interface IFrozenLookup<TKey, TValue> : IComparable<IFrozenLookup<TKey, TValue>>,
                                                   IEquatable<IFrozenLookup<TKey, TValue>>, IComparable where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        /// <summary>
        ///     Gets a count of the number of items in the dictionary.
        /// </summary>
        /// <value>
        ///     The number of items in the dictionary.
        /// </value>
        int Count { get; }

        /// <summary>
        ///     The keys identifying the values held in this dictionary.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        ///     The values held in this dictionary.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        IEnumerable<TValue> Values { get; }

        /// <summary>
        ///     Gets a snapshot of the values held in this dictionary.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        IEnumerable<TValue> ValuesSnapshot { get; }

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
        ///     Item not found.
        /// </exception>
        [JetBrains.Annotations.CanBeNull]
        TValue this[[JetBrains.Annotations.NotNull] TKey key] { get; set; }

        /// <summary>
        ///     Returns a clone of the current dictionary.
        /// </summary>
        /// <returns>
        ///     A clone of the dictionary given, including its existing values.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        IFrozenLookup<TKey, TValue> Clone();

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
        [JetBrains.Annotations.NotNull]
        IFrozenLookup<TKey, TValue> Clone(bool copyExistingValues);

        /// <summary>
        ///     Returns a clone of the dictionary given, with no values set and using <typeparamref name="TNew" /> as
        ///     the value type.
        /// </summary>
        /// <returns>
        ///     A clone of the current dictionary given.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        IFrozenLookup<TKey, TNew> Clone<TNew>();

        /// <summary>
        ///     Determines whether an item with the specified key exists in this dictionary.
        /// </summary>
        /// <param name="key">
        ///     The key for which to look.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if the specified key is found; otherwise, <see langword="false" />.
        /// </returns>
        bool ContainsKey([JetBrains.Annotations.CanBeNull] TKey key);

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
        ///     by the <see cref="FrozenLookup{TKey,TValue}.Count" /> property.
        /// </exception>
        [JetBrains.Annotations.NotNull]
        TKey KeyAt(int index);

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
        bool TryGetValue([JetBrains.Annotations.NotNull] TKey key, [JetBrains.Annotations.CanBeNull] out TValue value);

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
        ///     by the <see cref="FrozenLookup{TKey,TValue}.Count" /> property.
        /// </exception>
        [JetBrains.Annotations.CanBeNull]
        TValue ValueAt(int index);
    }
}