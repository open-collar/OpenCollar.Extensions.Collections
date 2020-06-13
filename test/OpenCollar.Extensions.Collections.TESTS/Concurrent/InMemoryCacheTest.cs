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
using System.Threading;

using OpenCollar.Extensions.Collections;
using OpenCollar.Extensions.Collections.Concurrent;

using Xunit;

namespace OpenCollar.Extensions.Collections.TESTS.Concurrent
{
    public class InMemoryCacheTest
    {
        private bool _success = false;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void CheckAutoFlush(int attempt)
        {
            const int maxAttempts = 5;
            const double autoFlushSeconds = 1d;

            // Don't retry if we don't have too.
            if(_success)
            {
                Assert.True(true);
                return;
            }

            var c = new InMemoryCache<string, DisposableMock>(TimeSpan.FromSeconds(autoFlushSeconds), s =>
            {
                var mock = new DisposableMock
                {
                    Id = s
                };
                return mock;
            }, true, false);
            const string key = @"TEST";
            var x1 = c[key];
            Assert.Equal(key, x1.Id);
            Thread.Sleep(TimeSpan.FromSeconds(autoFlushSeconds * 1.5d));

            // x1 hasn't been disposed of because AutoFlush is off.
            Assert.False(x1.IsDisposed);
            var x2 = c[key];

            // x1 will be disposed of now because it will have been cleared when the item was selected
            Assert.True(x1.IsDisposed);
            Assert.False(x2.IsDisposed);
            Assert.NotSame(x1, x2);

            // Now clear the cache and try again.
            c.Flush();
            c.Clear();
            c.Flush();
            var flushCount = 0;
            c.Flushed += (a, b) => { Interlocked.Increment(ref flushCount); };
            c.AutoDispose = true;
            c.AutoDispose = true;
            c.AutoFlush = true;
            c.AutoFlush = true; // The second call should take a different path - we can check in code coverage to ensure this is the case.
            Assert.True(c.AutoFlush);
            flushCount = 0;
            x1 = c[key];
            Assert.Equal(key, x1.Id);
            Assert.False(x1.IsDisposed);
            var loops = 0;
            const int maxLoops = 5;

            // Allow multiple iterations in case there is some thread starvation.
            while((loops++ < maxLoops) && (flushCount <= 0))
            {
                // While we are asleep all expired members of the cache should be automatically disposed of
                Thread.Sleep(TimeSpan.FromSeconds(autoFlushSeconds));
            }

            try
            {
                // Now x1 has been disposed, even though it hasn't been re-fetched.
                Assert.True(x1.IsDisposed, "Cached value has been disposed of.");

                // There has been a flush, without us needing to do anything.
                Assert.True(flushCount >= 1, "Auto-flush is responsible for disposing of the cached value.");

                _success = true;
            }
            catch
            {
                if(attempt >= maxAttempts)
                {
                    // Allow a couple of attempts because occasionally it fails for no reason.
                    throw;
                }
            }
        }
    }
}