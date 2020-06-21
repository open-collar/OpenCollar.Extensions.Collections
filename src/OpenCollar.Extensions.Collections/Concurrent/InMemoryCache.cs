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
using System.Linq;
using System.Threading;

using JetBrains.Annotations;

using OpenCollar.Extensions;
using OpenCollar.Extensions.Validation;

namespace OpenCollar.Extensions.Collections.Concurrent
{
    /// <summary>
    ///     A simple cache that allows objects to be created and reused with a TTL to control their maximum lifetime.
    /// </summary>
    /// <typeparam name="TKey">
    ///     The type of the key.
    /// </typeparam>
    /// <typeparam name="TItem">
    ///     The type of the item.
    /// </typeparam>
    /// <remarks>
    ///     <para>
    ///         Objects that implement the <see cref="IDisposable" /> interface will be disposed of if they expire.
    ///     </para>
    ///     <para>
    ///         If the <see cref="AutoFlush" /> property is set to <see langword="true" /> items in the cache are
    ///         actively flushed when they expire; otherwise they are only flushed when they are requested. Call the
    ///         <see cref="Flush" /> method to checked for expired items at any time.
    ///     </para>
    /// </remarks>
    public sealed class InMemoryCache<TKey, TItem> : Disposable where TKey : class, IEquatable<TKey>
    {
        /// <summary>
        ///     The value assigned to the <see cref="_autoFlush" /> field if auto-flush is disabled.
        /// </summary>
        private const int AUTOFLUSH_FALSE = 0;

        /// <summary>
        ///     The value assigned to the <see cref="_autoFlush" /> field if auto-flush is enabled.
        /// </summary>
        private const int AUTOFLUSH_TRUE = 1;

        /// <summary>
        ///     <para>
        ///         A flag indicating whether to automatically attempt to flush any stale items from the cache (to free resources).
        ///     </para>
        ///     <para>
        ///         Set to <see cref="AUTOFLUSH_TRUE" /> if stale items are to be automatically flushed; otherwise, <see cref="AUTOFLUSH_FALSE" />.
        ///     </para>
        /// </summary>
        private int _autoFlush = AUTOFLUSH_FALSE;

        /// <summary>
        ///     A dictionary of cached items, keyed on item key.
        /// </summary>
        /// <remarks>
        ///     Concurrent access is controlled by the <see cref="_lock" /> field.
        /// </remarks>
        [JetBrains.Annotations.NotNull]
        private readonly Dictionary<TKey, CacheItem> _cache = new Dictionary<TKey, CacheItem>();

        /// <summary>
        ///     The function used to create or refresh new instances of a cached value.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        private readonly Func<TKey, TItem> _create;

        /// <summary>
        ///     The lock used to control concurrent access to the <see cref="_cache" /> field.
        /// </summary>
        [JetBrains.Annotations.NotNull]
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        ///     The maximum permissible age of any item in the cache.
        /// </summary>
        private readonly TimeSpan _ttl;

        /// <summary>
        ///     The timer used to automatically check for expired items in the cache.
        /// </summary>
        [JetBrains.Annotations.CanBeNull]
        private Timer? _expiryTimer;

        /// <summary>
        ///     A flag used to track whether the <see cref="_expiryTimer" /> field has been populated.
        /// </summary>
        private bool _expiryTimerCreated;

        /// <summary>
        ///     The time at which the next automatic flush will be run.
        /// </summary>
        private DateTime _nextAutoFlushTime;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryCache{TKey,TItem}" /> class.
        /// </summary>
        /// <param name="ttl">
        ///     The maximum permissible age of any item in the cache.
        /// </param>
        /// <param name="create">
        ///     The function used to create or refresh new instances of a cached value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if the <paramref name="create" /> is <see langword="null" />.
        /// </exception>
        public InMemoryCache(TimeSpan ttl, [JetBrains.Annotations.NotNull] Func<TKey, TItem> create)
        {
            create.Validate(nameof(create), ObjectIs.NotNull);

            _create = create;
            _ttl = ttl;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryCache{TKey,TItem}" /> class.
        /// </summary>
        /// <param name="ttl">
        ///     The maximum permissible age of any item in the cache.
        /// </param>
        /// <param name="create">
        ///     The function used to create or refresh new instances of a cached value.
        /// </param>
        /// <param name="autoDispose">
        ///     <see langword="true" /> if disposable items should be disposed of when they expire; otherwise,
        ///     <see langword="false" />. <seealso cref="AutoDispose" />
        /// </param>
        public InMemoryCache(TimeSpan ttl, [JetBrains.Annotations.NotNull] Func<TKey, TItem> create, bool autoDispose) : this(ttl, create)
        {
            AutoDispose = autoDispose;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryCache{TKey,TItem}" /> class.
        /// </summary>
        /// <param name="ttl">
        ///     The maximum permissible age of any item in the cache.
        /// </param>
        /// <param name="create">
        ///     The function used to create or refresh new instances of a cached value.
        /// </param>
        /// <param name="autoDispose">
        ///     <see langword="true" /> if disposable items should be disposed of when they expire; otherwise,
        ///     <see langword="false" />. <seealso cref="AutoDispose" />
        /// </param>
        /// <param name="autoFlush">
        ///     If set to <see langword="true" /> the cache will automatically attempt to flush any stale items from the
        ///     cache (to free resources). <seealso cref="AutoFlush" />
        /// </param>
        public InMemoryCache(TimeSpan ttl, [JetBrains.Annotations.NotNull] Func<TKey, TItem> create, bool autoDispose, bool autoFlush) : this(ttl, create, autoDispose)
        {
            _autoFlush = autoFlush ? AUTOFLUSH_TRUE : AUTOFLUSH_FALSE;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether disposable items in the cache should be disposed of when they expire.
        /// </summary>
        /// <value>
        ///     <see langword="true" /> if disposable items should be disposed of when they expire; otherwise, <see langword="false" />.
        /// </value>
        /// <seealso cref="AutoFlush" />
        public bool AutoDispose { get; set; }

        /// <summary>
        ///     A flag indicating whether to automatically attempt to flush any stale items from the cache (to free resources).
        /// </summary>
        /// <seealso cref="AutoDispose" />
        public bool AutoFlush
        {
            get => _autoFlush != AUTOFLUSH_FALSE;

            set
            {
                CheckNotDisposed();

                if((_autoFlush == AUTOFLUSH_FALSE) && !value)
                {
                    return;
                }

                System.Threading.Interlocked.Exchange(ref _autoFlush, value ? AUTOFLUSH_TRUE : AUTOFLUSH_FALSE);

                UpdateAutoFlushTimer();
            }
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
        ///     <paramref name="key" /> must not be <see langword="null" />.
        /// </exception>
        [JetBrains.Annotations.CanBeNull]
        public TItem this[[JetBrains.Annotations.NotNull] TKey key]
        {
            get
            {
                CheckNotDisposed();

                key.Validate(nameof(key), ObjectIs.NotNull);

                // Our first attempt is from outside any locking
                if(!_cache.TryGetValue(key, out var cachedItem))
                {
                    // No luck? Then get exclusive access and try again.
                    _lock.EnterWriteLock();
                    try
                    {
                        // Someone else was in the process of creating it when we entered the lock.
                        if(!_cache.TryGetValue(key, out cachedItem))
                        {
                            // Still no luck, then we will need to create a cache entry
                            cachedItem = new CacheItem(_ttl, _create, this);
                            _cache.Add(key, cachedItem);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                Debug.Assert(cachedItem != null, "cachedItem != null");

                // Call the Item method from outside any locking - it will perform its own locking internally until the
                // item has been created.
                TItem item;
                if(cachedItem.GetItem(key, out item))
                {
                    UpdateAutoFlushTimer(); // if this is a newly created item we need to make sure that the auto-flush timer has been correctly set.
                }

                return item;
            }
        }

        /// <summary>
        ///     Occurs when cached items are flushed and one or more items have been removed.
        /// </summary>
        public event EventHandler<EventArgs>? Flushed;

        /// <summary>
        ///     Clears all existing items from the cache. If <see cref="AutoDispose" /> is <see langword="true" /> the
        ///     cached items that are removed will be disposed of if possible.
        /// </summary>
        /// <remarks>
        ///     The cached items cleared from the array will be disposed of asynchronously.
        /// </remarks>
        public void Clear()
        {
            _lock.EnterWriteLock();
            CacheItem[] items;
            try
            {
                items = _cache.Values.ToArray();
                _cache.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach(var item in items)
                {
                    Debug.Assert(item != null, "item != null");
                    item.Dispose();
                }
            });
        }

        /// <summary>
        ///     Deletes the specified item from the cache, forcing it to be recreated the next time it is requested.
        /// </summary>
        /// <param name="item">
        ///     The item to delete from the cache.
        /// </param>
        public void Delete(TItem item)
        {
            CheckNotDisposed();
            TKey key = null;
            _lock.EnterUpgradeableReadLock();
            try
            {
                foreach(var wrapper in _cache)
                {
                    Debug.Assert(wrapper.Value != null, "wrapper.Value != null");
                    if(ReferenceEquals(wrapper.Value.CurrentItem, item))
                    {
                        key = wrapper.Key;
                        break;
                    }
                }

                if(key != null)
                {
                    Delete(key);
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///     Flushes all expired items from the cache.
        /// </summary>
        /// <remarks>
        ///     If the <see cref="AutoDispose" /> flag is set then any items that implement the
        ///     <see cref="IDisposable" /> interface will be disposed of.
        /// </remarks>
        /// <seealso cref="AutoDispose" />
        /// /// /// ///
        /// <seealso cref="AutoFlush" />
        public void Flush()
        {
            CheckNotDisposed();
            var flushedItemCount = 0;
            _lock.EnterWriteLock();
            try
            {
                foreach(var record in _cache.ToArray())
                {
                    Debug.Assert(record.Value != null, "record.Value != null");
                    if(record.Value.CanFlush())
                    {
                        Debug.Assert(record.Key != null, "record.Key != null");
                        _cache.Remove(record.Key);
                        record.Value.Dispose();
                        ++flushedItemCount;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            UpdateAutoFlushTimer();
            if(flushedItemCount > 0)
            {
                // Take a copy to ensure thread safety.
                var flushed = Flushed;
                flushed?.Invoke(this, EventArgs.Empty);
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
                    if(_expiryTimerCreated)
                    {
                        Debug.Assert(_expiryTimer != null, "_expiryTimer != null");
                        _expiryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _expiryTimer.Dispose();
                    }
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
        ///     Deletes the item associated with the key specified from the cache, forcing it to be recreated the next
        ///     time it is requested.
        /// </summary>
        /// <param name="key">
        ///     The key identifying the item to delete from the cache.
        /// </param>
        public void Delete([JetBrains.Annotations.NotNull] TKey key)
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
        ///     A property that is used access the expiry timer (and to ensure the expiry timer has been instantiated).
        /// </summary>
        /// <value>
        ///     The expiry timer.
        /// </value>
        [JetBrains.Annotations.NotNull]
        private Timer GetExpiryTimer(bool writeLockAcquired)
        {
            CheckNotDisposed();
            if(_expiryTimerCreated)
            {
                Debug.Assert(_expiryTimer != null, "_expiryTimer != null");
                return _expiryTimer;
            }

            if(!writeLockAcquired)
            {
                _lock.EnterUpgradeableReadLock();
            }

            try
            {
                if(_expiryTimerCreated)
                {
                    Debug.Assert(_expiryTimer != null, "_expiryTimer != null");
                    return _expiryTimer;
                }

                if(!writeLockAcquired)
                {
                    _lock.EnterWriteLock();
                }

                try
                {
                    _expiryTimer = new Timer(OnExpire);
                    _expiryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _expiryTimerCreated = true;
                    return _expiryTimer;
                }
                finally
                {
                    if(!writeLockAcquired)
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                if(!writeLockAcquired)
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }
        }

        /// <summary>
        ///     Called when the expiry timer has fired, indicating that the cache must be checked.
        /// </summary>
        /// <param name="state">
        ///     The state.
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This code must not raise any exceptions.")]
        private void OnExpire(object state)
        {
            if(IsDisposed)
            {
                return;
            }

            try
            {
                // Prevent the timer from firing until after we have flushed.
                GetExpiryTimer(false).Change(Timeout.Infinite, Timeout.Infinite);
                Flush();
            }
            catch(Exception ex)
            {
                ExceptionManager.OnUnhandledException(ex);
            }
        }

        /// <summary>
        ///     Updates the automatic flush timer to fire when the next expiry time becomes due.
        /// </summary>
        private void UpdateAutoFlushTimer()
        {
            var autoFlush = _autoFlush == AUTOFLUSH_TRUE;

            // Don't do anything if there is nothing to do
            if(IsDisposed || (!autoFlush && !_expiryTimerCreated))
            {
                return;
            }

            _lock.EnterUpgradeableReadLock();
            try
            {
                DateTime nextExpiry;

                nextExpiry = autoFlush ? GetNextExpireTime() : DateTime.MaxValue;

                if(_nextAutoFlushTime != nextExpiry)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        SetAutoFlushTimer(nextExpiry);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///     Sets the automatic flush timer using the expiry time given.
        /// </summary>
        /// <param name="nextExpiry">
        ///     The next expiry time.
        /// </param>
        private void SetAutoFlushTimer(DateTime nextExpiry)
        {
            var timer = GetExpiryTimer(true);
            if(nextExpiry >= DateTime.MaxValue)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                var time = nextExpiry - DateTime.UtcNow;
                if(time.TotalMilliseconds <= 1)
                {
                    time = TimeSpan.FromMilliseconds(1000);
                }

                timer.Change(time, time);
            }

            _nextAutoFlushTime = nextExpiry;
        }

        /// <summary>
        ///     Gets the next expiry time.
        /// </summary>
        /// <returns>
        ///     The next expiry time or <see cref="System.DateTime.MaxValue" /> if the cache is empty.
        /// </returns>
        private DateTime GetNextExpireTime()
        {
            var nextExpiry = DateTime.MaxValue;

            foreach(var record in _cache.ToArray())
            {
                Debug.Assert(record.Value != null, "record.Value != null");
                if(record.Value.ExpireTime < nextExpiry)
                {
                    nextExpiry = record.Value.ExpireTime;
                }
            }

            return nextExpiry;
        }

        /// <summary>
        ///     An item in a cache
        /// </summary>
        private sealed class CacheItem : Disposable
        {
            /// <summary>
            ///     The function used to create or refresh new instances of a cached value.
            /// </summary>
            [JetBrains.Annotations.NotNull]
            private readonly Func<TKey, TItem> _create;

            /// <summary>
            ///     The lock used to control concurrent access to the <see cref="InMemoryCache{TKey,TItem}._cache" /> field.
            /// </summary>
            [JetBrains.Annotations.NotNull]
#pragma warning disable CA2213 // Disposable fields should be disposed
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

#pragma warning restore CA2213 // Disposable fields should be disposed

            /// <summary>
            ///     The cache to which this item belongs.
            /// </summary>
            [JetBrains.Annotations.NotNull]
            private readonly InMemoryCache<TKey, TItem> _parent;

            /// <summary>
            ///     The length of time for which instantiated items are valid.
            /// </summary>
            private readonly TimeSpan _ttl;

            /// <summary>
            ///     The time at which the cached item exceeds its TTL and must be replaced (in UTC)
            /// </summary>
            private DateTime _expireTime;

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
            ///     Initializes a new instance of the <see cref="CacheItem" /> class.
            /// </summary>
            /// <param name="ttl">
            ///     The maximum age of any item in the cache.
            /// </param>
            /// <param name="createFunction">
            ///     The function used to create a new object.
            /// </param>
            /// <param name="parent">
            ///     The cache to which this item belongs.
            /// </param>
            public CacheItem(TimeSpan ttl, [JetBrains.Annotations.NotNull] Func<TKey, TItem> createFunction, [JetBrains.Annotations.NotNull] InMemoryCache<TKey, TItem> parent)
            {
                _ttl = ttl;
                _create = createFunction;
                _parent = parent;
                _instantiated = false;
                _expireTime = DateTime.UtcNow + ttl;
            }

            /// <summary>
            ///     The time at which the cached item exceeds its TTL and must be replaced (in UTC)
            /// </summary>
            public DateTime ExpireTime => _expireTime;

            /// <summary>
            ///     Gets the current item, without checking for expiry etc.
            /// </summary>
            /// <value>
            ///     The current item.
            /// </value>
            [JetBrains.Annotations.CanBeNull]
            internal TItem CurrentItem => _item;

            /// <summary>
            ///     Checks to see if the cached item can be flushed, and if so returns <see langword="true" />;
            ///     otherwise returns <see langword="false" />.
            /// </summary>
            /// <returns>
            ///     <see langword="true" /> if the cached item can been flushed; otherwise <see langword="false" />.
            /// </returns>
            public bool CanFlush()
            {
                if(!_instantiated)
                {
                    return true;
                }

                _lock.EnterReadLock();
                try
                {
                    return _expireTime <= DateTime.UtcNow;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            /// <summary>
            ///     Gets the cached item.
            /// </summary>
            /// <param name="key">
            ///     The key.
            /// </param>
            /// <param name="item">
            ///     The item requested.
            /// </param>
            /// <returns>
            ///     <see langword="true" /> if the item is newly created; otherwise <see langword="false" /> if it was cached.
            /// </returns>
            /// <remarks>
            ///     If an item expires and the <see cref="AutoDispose" /> flag is set then if the item implement the
            ///     <see cref="IDisposable" /> interface it will be disposed of.
            /// </remarks>
            /// <seealso cref="AutoDispose" />
            /// /// /// ///
            /// <seealso cref="AutoFlush" />
            public bool GetItem([JetBrains.Annotations.NotNull] TKey key, [JetBrains.Annotations.CanBeNull] out TItem item)
            {
                // Create the object on demand, blocking whilst doing so.

                // A quick check before we go any further into acquiring exclusive access
                if(_instantiated && (_expireTime > DateTime.UtcNow))
                {
                    item = _item;
                    return false;
                }

                _lock.EnterWriteLock();
                try
                {
                    // Check again in case another thread has already done the work whilst we were waiting.
                    if(_instantiated && (_expireTime > DateTime.UtcNow))
                    {
                        item = _item;
                        return false;
                    }

                    if(_instantiated && _parent.AutoDispose)
                    {
                        var disposable = _item as IDisposable;
                        disposable?.Dispose();
                    }

                    _instantiated = false;
                    _item = _create(key);
                    _expireTime = DateTime.UtcNow + _ttl;
                    _instantiated = true;
                    item = _item;
                    return true;
                }
                finally
                {
                    _lock.ExitWriteLock();
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
                        if(_parent.AutoDispose)
                        {
                            var disposable = _item as IDisposable;
                            disposable?.Dispose();
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    _lock.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}