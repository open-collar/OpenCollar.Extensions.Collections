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

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     The interface shared by all objects that act as proxies for items in a reference counted cache.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IReferenceCountingCacheItemProxy : IDisposable, IEquatable<IReferenceCountingCacheItemProxy>
    {
        /// <summary>
        ///     Gets the token that represents related proxy objects.
        /// </summary>
        /// <value>
        ///     The token.
        /// </value>
        [JetBrains.Annotations.NotNull]
        IComparable Token { get; }

        /// <summary>
        ///     Returns a reference-counted clone of this instance.
        /// </summary>
        /// <returns>
        ///     A reference-counted clone of this instance.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        IReferenceCountingCacheItemProxy Clone();
    }
}