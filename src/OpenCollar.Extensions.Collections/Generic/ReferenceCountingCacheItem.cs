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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using JetBrains.Annotations;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     An item in a reference counted cache
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification =
        "Unavoidable.")]
    public sealed class ReferenceCountingCacheItem<TKey, TItem, TProxy> : Disposable
        where TKey : class, IEquatable<TKey>
        where TProxy : ReferenceCountingCache<TKey, TItem, TProxy>.ReferenceCountingCacheItemProxy,
        IReferenceCountingCacheItemProxy
    {
        /// <summary>
        ///     The function used to create or refresh new instances of a cached value.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly Func<TKey, TItem> _createItem;

        /// <summary>
        ///     The key identifying the item.
        /// </summary>
        private readonly TKey _key;

        /// <summary>
        ///     The lock used to control concurrent access to the
        ///     <see cref="ReferenceCountingCache{TKey,TItem,TProxy}._cache" /> field.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        ///     <see langword="true" /> if the item has been set.
        /// </summary>
        private bool _instantiated;

        /// <summary>
        ///     The cached item.
        /// </summary>
        [JetBrains.Annotations.CanBeNull]
        private TItem _item;

        /// <summary>
        ///     A count of the number of proxies referring to this item.
        /// </summary>
        private int _referenceCount;

        /// <summary>
        ///     Occurs when a cache item disposed has been disposed because it has no more references.
        /// </summary>
        internal event EventHandler CacheItemDisposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReferenceCountingCacheItem{T,T,T}" /> class.
        /// </summary>
        /// <param name="createItemFunction">
        ///     The function used to create a new object.
        /// </param>
        /// <param name="key">
        ///     The key identifying the item.
        /// </param>
        internal ReferenceCountingCacheItem([JetBrains.Annotations.NotNull] Func<TKey, TItem> createItemFunction, TKey key)
        {
            _createItem = createItemFunction;
            _key = key;
            _instantiated = false;

            Token = Guid.NewGuid();
        }

        /// <summary>
        ///     Gets the token that represents related proxy objects.
        /// </summary>
        /// <value>
        ///     The token that represents related proxy objects.
        /// </value>
        [JetBrains.Annotations.NotNull]
        public IComparable Token { get; }

        /// <summary>
        ///     Gets the current item, without checking for expiry etc.
        /// </summary>
        /// <value>
        ///     The current item.
        /// </value>
        [JetBrains.Annotations.CanBeNull]
        internal TItem CurrentItem => _item;

        /// <summary>
        ///     Gets the key identifying the item.
        /// </summary>
        /// <value>
        ///     The key identifying the item.
        /// </value>
        [JetBrains.Annotations.NotNull]
        internal TKey Key => _key;

        /// <summary>
        ///     Increments the reference count for the item represented.
        /// </summary>
        internal void AddReference()
        {
            _lock.EnterWriteLock();
            try
            {
                ++_referenceCount;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Gets the cached item.
        /// </summary>
        /// <returns>
        ///     The cached item.
        /// </returns>
        [JetBrains.Annotations.CanBeNull]
        internal TItem GetItem()
        {
            // Create the object on demand, blocking whilst doing so.

            // A quick check before we go any further into acquiring exclusive access
            if(_instantiated)
                return _item;

            _lock.EnterWriteLock();
            try
            {
                _item = _createItem(_key);
                _instantiated = true;
                return _item;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Decrements the reference count for the item represented.
        /// </summary>
        internal void RemoveReference()
        {
            TItem item;

            _lock.EnterWriteLock();
            try
            {
                --_referenceCount;
                if(_referenceCount > 0)
                    return;

                item = _item;
                _item = default(TItem);
                _instantiated = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            var disposable = item as IDisposable;
            if(!ReferenceEquals(disposable, null))
                disposable.Dispose();

            OnCacheItemDisposed(EventArgs.Empty);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
        ///     release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _lock.EnterWriteLock();
                try
                {
                    var disposable = _item as IDisposable;
                    _instantiated = false;
                    disposable?.Dispose();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                _lock.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Raises the <see cref="E:CacheItemDisposed" /> event.
        /// </summary>
        /// <param name="args">
        ///     The <see cref="EventArgs" /> instance containing the event data.
        /// </param>
        private void OnCacheItemDisposed(EventArgs args)
        {
            var handler = CacheItemDisposed;
            handler?.Invoke(this, args);
        }
    }
}