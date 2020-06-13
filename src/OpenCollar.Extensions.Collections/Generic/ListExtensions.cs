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

using OpenCollar.Extensions.Validation;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     Extensions for the <See cref="IList{T}" /> type.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        ///     Synchronizes the contents of two lists using the minimum changes.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the elements in the lists.
        /// </typeparam>
        /// <param name="source">
        ///     The lists that defines the correct contents.
        /// </param>
        /// <param name="target">
        ///     The target list that will be synchronized.
        /// </param>
        /// <remarks>
        ///     The comparison used to identified unchanged items uses <see cref="object.ReferenceEquals" />, to perform
        ///     a custom comparison use the <see cref="Synchronize{T}(IList{T},IList{T},Func{T,T,bool})" /> overload of
        ///     this method.
        /// </remarks>
        public static void Synchronize<T>([JetBrains.Annotations.NotNull] this IList<T> target, [JetBrains.Annotations.NotNull] IList<T> source) where T : class
        {
            target.Validate(nameof(target), ObjectIs.NotNull);
            source.Validate(nameof(source), ObjectIs.NotNull);
            if(ReferenceEquals(target, source))
                return;

            var sourceCount = source.Count;
            var targetCount =
                target.Count; // The target count may change during processing, but we always want to deal with the original value.

            for(var n = 0; (n < sourceCount) || (n < targetCount); ++n)
            {
                if(n < sourceCount)
                    if(n < targetCount)
                    {
                        if(!ReferenceEquals(target[n], source[n]))
                            target[n] = source[n];
                    }
                    else
                    {
                        // Add missing elements from the end
                        target.Add(source[n]);
                    }
                else
                    target.RemoveAt(target.Count - 1);
            }
        }

        /// <summary>
        ///     Synchronizes the contents of two lists using the minimum changes.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the elements in the lists.
        /// </typeparam>
        /// <param name="target">
        ///     The target list that will be synchronized.
        /// </param>
        /// <param name="source">
        ///     The lists that defines the correct contents.
        /// </param>
        /// <param name="equals">
        ///     The function to use when comparing elements.
        /// </param>
        /// <remarks>
        ///     To use the <see cref="object.ReferenceEquals" /> function to compare elements use the
        ///     <see cref="Synchronize{T}(IList{T}, IList{T})" /> overload of this method.
        /// </remarks>
        public static void Synchronize<T>([JetBrains.Annotations.NotNull] this IList<T> target, [JetBrains.Annotations.NotNull] IList<T> source,
                                          [JetBrains.Annotations.NotNull] Func<T, T, bool> equals)
        {
            target.Validate(nameof(target), ObjectIs.NotNull);
            source.Validate(nameof(source), ObjectIs.NotNull);
            equals.Validate(nameof(equals), ObjectIs.NotNull);
            if(ReferenceEquals(target, source))
                return;

            var sourceCount = source.Count;
            var targetCount =
                target.Count; // The target count may change during processing, but we always want to deal with the original value.

            for(var n = 0; (n < sourceCount) || (n < targetCount); ++n)
            {
                if(n < sourceCount)
                    if(n < targetCount)
                    {
                        if(!equals(target[n], source[n]))
                            target[n] = source[n];
                    }
                    else
                    {
                        // Add missing elements from the end
                        target.Add(source[n]);
                    }
                else
                    target.RemoveAt(target.Count - 1);
            }
        }
    }
}