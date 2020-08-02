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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

using OpenCollar.Extensions.Validation;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     A simple cache that allows objects to be created and reused with referencing counting used to determine
    ///     their lifetime.
    /// </summary>
    /// <typeparam name="TKey">
    ///     The type of the key.
    /// </typeparam>
    /// <typeparam name="TItem">
    ///     The type of the item.
    /// </typeparam>
    /// <typeparam name="TProxy">
    ///     The type of the item proxy.
    /// </typeparam>
    /// <remarks>
    ///     <para>
    ///         Objects that implement the <see cref="IDisposable" /> interface will be disposed of if they expire.
    ///     </para>
    /// </remarks>
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification =
        "Unavoidable.")]
    public sealed class ReferenceCountingCache<TKey, TItem, TProxy> : Disposable where TKey : class, IEquatable<TKey>
                                                                                 where TProxy : ReferenceCountingCache<TKey, TItem, TProxy>.
                                                                                 ReferenceCountingCacheItemProxy,
                                                                                 IReferenceCountingCacheItemProxy
    {
        /// <summary>
        ///     A dictionary of cached items, keyed on item key.
        /// </summary>
        /// <remarks>
        ///     Concurrent access is controlled by the <see cref="_lock" /> field.
        /// </remarks>
        [JetBrains.Annotations.NotNull]
        private readonly Dictionary<TKey, ReferenceCountingCacheItem<TKey, TItem, TProxy>> _cache =
            new Dictionary<TKey, ReferenceCountingCacheItem<TKey, TItem, TProxy>>();

        /// <summary>
        ///     The function used to create new instances of a cached item.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly Func<TKey, TItem> _createItem;

        /// <summary>
        ///     The lock used to control concurrent access to the <see cref="_cache" /> field.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        ///     The function used to create a new proxy for an existing cached item.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly Func<ReferenceCountingCacheItem<TKey, TItem, TProxy>, TProxy> _proxyFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReferenceCountingCache{TKey, TContent, TProxy}" /> class.
        /// </summary>
        /// <param name="createItem">
        ///     The function used to create new instances of a cached item.
        /// </param>
        /// <param name="proxyFactory">
        ///     The function used to create a new proxy for an existing cached item.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="createItem" /> or <paramref name="proxyFactory" /> is <see langword="null" />.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification =
            "Unavoidable")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ttl")]
        public ReferenceCountingCache([JetBrains.Annotations.NotNull] Func<TKey, TItem> createItem,
                                      [JetBrains.Annotations.NotNull] Func<ReferenceCountingCacheItem<TKey, TItem, TProxy>, TProxy> proxyFactory)
        {
            createItem.Validate(nameof(createItem), ObjectIs.NotNull);
            proxyFactory.Validate(nameof(proxyFactory), ObjectIs.NotNull);

            _createItem = createItem;
            _proxyFactory = proxyFactory;
        }

        /// <summary>
        ///     Gets the item with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key identifying the object required.
        /// </param>
        /// <returns>
        ///     The object requested.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="key" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="_proxyFactory" /> delegate returned <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     Object cannot be accessed after it has been disposed of.
        /// </exception>
        [JetBrains.Annotations.CanBeNull]
        public TProxy this[[JetBrains.Annotations.NotNull] TKey key]
        {
            get
            {
                CheckNotDisposed();

                key.Validate(nameof(key), ObjectIs.NotNull);

                // Our first attempt is from outside any locking
                ReferenceCountingCacheItem<TKey, TItem, TProxy> cachedItem;
                if(!_cache.TryGetValue(key, out cachedItem))
                {
                    // No luck? Then get exclusive access and try again.
                    _lock.EnterWriteLock();
                    try
                    {
                        // Someone else was in the process of creating it when we entered the lock.
                        if(!_cache.TryGetValue(key, out cachedItem))
                        {
                            // Still no luck, then we will need to create a cache entry
                            cachedItem = new ReferenceCountingCacheItem<TKey, TItem, TProxy>(_createItem, key);
                            _cache.Add(key, cachedItem);
                            cachedItem.CacheItemDisposed += CachedItemDisposed;
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                Debug.Assert(cachedItem != null, "cachedItem != null");

                // Call the GetItem method from outside any locking - it will perform its own locking internally until
                // the item has been created.
                var proxy = _proxyFactory(cachedItem);
                if(proxy == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      Resources.Exceptions.DelegateReturnedNull, "_proxyFactory"));
                proxy.ProxyFactory = _proxyFactory;
                return proxy;
            }
        }

        /// <summary>
        ///     Deletes the specified item from the cache, forcing it to be recreated the next time it is requested.
        /// </summary>
        /// <param name="item">
        ///     The item to delete from the cache.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        ///     Object cannot be accessed after it has been disposed of.
        /// </exception>
        public void Delete(TItem item)
        {
            CheckNotDisposed();

            TKey key = null;

            _lock.EnterUpgradeableReadLock();
            try
            {
                foreach(var wrapper in _cache)

                // ReSharper disable once PossibleNullReferenceException
                {
                    if(ReferenceEquals(wrapper.Value.CurrentItem, item))
                    {
                        key = wrapper.Key;
                        break;
                    }
                }

                if(key != null)
                    Delete(key);
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
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
                    foreach(var item in _cache.Values)
                    {
                        Debug.Assert(item != null, "item != null");
                        item.Dispose();
                    }

                    _cache.Clear();
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
        ///     Handles the cached item disposed event of a cached item.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="EventArgs" /> instance containing the event data.
        /// </param>
        private void CachedItemDisposed([JetBrains.Annotations.CanBeNull] object sender, [JetBrains.Annotations.NotNull] EventArgs e)
        {
            var cachedItem = sender as ReferenceCountingCacheItem<TKey, TItem, TProxy>;
            if(cachedItem == null)
                return;

            // Unhook this event handler and delete the item from the cache
            cachedItem.CacheItemDisposed -= CachedItemDisposed;
            Delete(cachedItem.Key);
        }

        /// <summary>
        ///     Deletes the item associated with the key specified from the cache, forcing it to be recreated the next
        ///     time it is requested.
        /// </summary>
        /// <param name="key">
        ///     The key identifying the item to delete from the cache.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        ///     Object cannot be accessed after it has been disposed of.
        /// </exception>
        private void Delete([JetBrains.Annotations.NotNull] TKey key)
        {
            CheckNotDisposed();

            _lock.EnterWriteLock();
            try
            {
                _cache.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     A base class for proxies representing a reference-counted cached item.
        /// </summary>
        /// <remarks>
        ///     Use the <see cref="IDisposable.Dispose" /> method to decrement the reference count held in the
        ///     underlying cache.
        /// </remarks>
        /// <seealso cref="IDisposable" />
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification =
            "Logically associated with parent class, it doesn't make sense to separate it.")]
        public abstract class ReferenceCountingCacheItemProxy : Disposable, IReferenceCountingCacheItemProxy
        {
            /// <summary>
            ///     The _token
            /// </summary>
            [JetBrains.Annotations.NotNull]
            private readonly IComparable _token;

            /// <summary>
            ///     The cache item representing the item for which we are being a proxy.
            /// </summary>
            [JetBrains.Annotations.NotNull]
            private ReferenceCountingCacheItem<TKey, TItem, TProxy> _cacheItem;

            /// <summary>
            ///     The function used to create a new proxy for an existing cached item.
            /// </summary>
            [JetBrains.Annotations.NotNull]
            private Func<ReferenceCountingCacheItem<TKey, TItem, TProxy>, TProxy> _proxyFactory;

            /// <summary>
            ///     Initializes a new instance of the <see cref="ReferenceCountingCacheItemProxy" /> class.
            /// </summary>
            /// <param name="cacheItem">
            ///     The cache item that this proxy will used to access the underlying object represented.
            /// </param>
            /// <exception cref="ArgumentNullException">
            ///     <paramref name="cacheItem" /> is <see langword="null" />.
            /// </exception>
            // Disabled warning about non-null member (_cacheItem) being set as it will always be set before use.
            // ReSharper disable once NotNullMemberIsNotInitialized
            protected ReferenceCountingCacheItemProxy(
                [JetBrains.Annotations.NotNull] ReferenceCountingCacheItem<TKey, TItem, TProxy> cacheItem)
            {
                cacheItem.Validate(nameof(cacheItem), ObjectIs.NotNull);

                _cacheItem = cacheItem;
                _cacheItem.AddReference();
                _token = _cacheItem.Token;
            }

            /// <summary>
            ///     Gets the token that represents related proxy objects.
            /// </summary>
            /// <value>
            ///     The token that represents related proxy objects.
            /// </value>
            public IComparable Token => _token;

            /// <summary>
            ///     The function used to create a new proxy for an existing cached item.
            /// </summary>
            /// <remarks>
            ///     Set immediately after construction.
            /// </remarks>
            /// <exception cref="ArgumentNullException">
            ///     <paramref name="value" /> is <see langword="null" />.
            /// </exception>
            [JetBrains.Annotations.NotNull]
            internal Func<ReferenceCountingCacheItem<TKey, TItem, TProxy>, TProxy> ProxyFactory
            {
                get => _proxyFactory;

                set
                {
                    value.Validate(nameof(value), ObjectIs.NotNull);

                    _proxyFactory = value;
                }
            }

            /// <summary>
            ///     Gets the underlying item to be represented by this proxy.
            /// </summary>
            /// <value>
            ///     The base item represented by this proxy.
            /// </value>
            /// <exception cref="ObjectDisposedException">
            ///     Object cannot be accessed after it has been disposed of.
            /// </exception>
            protected TItem BaseItem
            {
                get
                {
                    CheckNotDisposed();

                    return _cacheItem.GetItem();
                }
            }

            /// <summary>
            ///     Gets the underlying item to be represented by this proxy.
            /// </summary>
            /// <value>
            ///     The base item represented by this proxy.
            /// </value>
            protected TItem BaseItemUnchecked => _cacheItem.GetItem();

            /// <summary>
            ///     Implements the != operator.
            /// </summary>
            /// <param name="left">
            ///     The left-hand operand.
            /// </param>
            /// <param name="right">
            ///     The right-hand operand.
            /// </param>
            /// <returns>
            ///     <see langword="true" /> if the two arguments are not equal; otherwise, <see langword="false" />.
            /// </returns>
            public static bool operator !=([JetBrains.Annotations.CanBeNull] ReferenceCountingCacheItemProxy left,
                                           [JetBrains.Annotations.CanBeNull] ReferenceCountingCacheItemProxy right)
            {
                return !Equals(left, right);
            }

            /// <summary>
            ///     Implements the == operator.
            /// </summary>
            /// <param name="left">
            ///     The left-hand operand.
            /// </param>
            /// <param name="right">
            ///     The right-hand operand.
            /// </param>
            /// <returns>
            ///     <see langword="true" /> if the two arguments are equal; otherwise, <see langword="false" />.
            /// </returns>
            public static bool operator ==([JetBrains.Annotations.CanBeNull] ReferenceCountingCacheItemProxy left,
                                           [JetBrains.Annotations.CanBeNull] ReferenceCountingCacheItemProxy right)
            {
                return Equals(left, right);
            }

            /// <summary>
            ///     Returns a reference-counted clone of this instance.
            /// </summary>
            /// <returns>
            ///     A reference-counted clone of this instance.
            /// </returns>
            /// <exception cref="ObjectDisposedException">
            ///     Object cannot be accessed after it has been disposed of.
            /// </exception>
            /// <exception cref="InvalidOperationException">
            ///     The <see cref="ProxyFactory" /> delegate returned <see langword="null" />.
            /// </exception>
            public IReferenceCountingCacheItemProxy Clone()
            {
                CheckNotDisposed();

                var proxy = _proxyFactory(_cacheItem);
                if(proxy == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      Resources.Exceptions.DelegateReturnedNull, "ProxyFactory"));
                proxy.ProxyFactory = _proxyFactory;
                return proxy;
            }

            /// <summary>
            ///     Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <returns>
            ///     true <see langword="true" /> if the current object is equal to the <paramref name="other" />
            ///     parameter; otherwise, <see langword="false" />.
            /// </returns>
            /// <param name="other">
            ///     An object to compare with this object.
            /// </param>
            public bool Equals(IReferenceCountingCacheItemProxy other)
            {
                return Equals(this, other);
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
                return Equals(this, obj as IReferenceCountingCacheItemProxy);
            }

            /// <summary>
            ///     Serves as a hash function for a particular type.
            /// </summary>
            /// <returns>
            ///     A hash code for the current <see cref="System.Object" />.
            /// </returns>
            public override int GetHashCode()
            {
                return _token.GetHashCode();
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
                    _cacheItem.RemoveReference();

                    // ReSharper disable AssignNullToNotNullAttribute
                    _proxyFactory = null;
                    _cacheItem = null;

                    // ReSharper restore AssignNullToNotNullAttribute
                }

                base.Dispose(disposing);
            }

            /// <summary>
            ///     Determines whether the two <see cref="IReferenceCountingCacheItemProxy" /> instances are equal.
            /// </summary>
            /// <param name="first">
            ///     The first instance to compare.
            /// </param>
            /// <param name="second">
            ///     The second instance to compare.
            /// </param>
            /// <returns>
            ///     <see langword="true" /> if the two <see cref="IReferenceCountingCacheItemProxy" /> instances are
            ///     equal; otherwise, <see langword="false" />.
            /// </returns>
            private static bool Equals([JetBrains.Annotations.CanBeNull] IReferenceCountingCacheItemProxy first,
                                       [JetBrains.Annotations.CanBeNull] IReferenceCountingCacheItemProxy second)
            {
                if(ReferenceEquals(first, second))
                    return true;

                return !ReferenceEquals(first, null) && !ReferenceEquals(second, null) &&
                       ReferenceEquals(first.Token, second.Token);
            }
        }
    }
}