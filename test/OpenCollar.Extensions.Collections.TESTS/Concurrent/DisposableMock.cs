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

using JetBrains.Annotations;

namespace OpenCollar.Extensions.Collections.TESTS.Concurrent
{
    public sealed class DisposableMock : Disposable
    {
        private int _callCount;

        private bool _disposing;

        public int CallCount => _callCount;

        public bool Disposing => _disposing;

        public bool FailOnDispose { get; set; }

        public string Id { get; set; }

        [JetBrains.Annotations.NotNull]
        public AutoResetEvent LockEvent { get; } = new AutoResetEvent(true);

        public void PerformCheck()
        {
            CheckNotDisposed();
        }

        protected override void Dispose(bool disposing)
        {
            ++_callCount;
            _disposing = disposing;
            if(FailOnDispose)
            {
                throw new Exception();
            }

            // If necessary wait for other resources to be ready.
            LockEvent.WaitOne();
            base.Dispose(disposing);
        }
    }
}