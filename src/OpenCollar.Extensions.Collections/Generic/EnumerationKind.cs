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

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     The ways in which a sequence can be safely enumerated.
    /// </summary>
    public enum EnumerationKind
    {
        /// <summary>
        ///     <see langword="null" /> elements are included.
        /// </summary>
        IncludeNulls = 0,

        /// <summary>
        ///     <see langword="null" /> elements are not included in the sequence returned.
        /// </summary>
        ExcludeNulls
    }
}