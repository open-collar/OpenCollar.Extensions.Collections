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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

using OpenCollar.Extensions.Validation;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     Extensions to the <see cref="IEnumerable{T}" /> type and related methods.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        ///     Allows any sequence to be safely enumerated, even if it is in fact <see langword="null" />.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the elements in the sequence to enumerate.
        /// </typeparam>
        /// <param name="sequence">
        ///     The sequence to enumerate.
        /// </param>
        /// <returns>
        ///     The sequence given, if it is not <see langword="null" />, otherwise an empty sequence of the same type.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemCanBeNull]
        public static IEnumerable<T> EnumerateSafely<T>([JetBrains.Annotations.CanBeNull] this IEnumerable<T> sequence)
        {
            if(ReferenceEquals(sequence, null))
            {
                return Enumerable.Empty<T>();
            }

            return sequence;
        }

        /// <summary>
        ///     Allows any sequence to be safely enumerated, even if it is in fact <see langword="null" />.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the elements in the sequence to enumerate.
        /// </typeparam>
        /// <param name="sequence">
        ///     The sequence to enumerate.
        /// </param>
        /// <param name="enumerationKind">
        ///     The kind of the enumeration to perform.
        /// </param>
        /// <returns>
        ///     The sequence given, if it is not <see langword="null" />, otherwise an empty sequence of the same type.
        ///     <see langword="null" /> elements will not be emitted if <paramref name="enumerationKind" /> is <see cref="EnumerationKind.ExcludeNulls" />.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemCanBeNull]
        public static IEnumerable<T> EnumerateSafely<T>([JetBrains.Annotations.CanBeNull] this IEnumerable<T> sequence, EnumerationKind enumerationKind)
        {
            if(ReferenceEquals(sequence, null))
            {
                yield break;
            }

            foreach(var item in sequence)
            {
                if(ReferenceEquals(item, null) && (enumerationKind == EnumerationKind.ExcludeNulls))
                {
                    continue;
                }
#pragma warning disable S2259
                yield return item;
#pragma warning restore S2259
            }
        }

        /// <summary>
        ///     Allows any sequence to be safely enumerated, even if it is in fact <see langword="null" />.
        /// </summary>
        /// <param name="sequence">
        ///     The sequence to enumerate.
        /// </param>
        /// <returns>
        ///     The sequence given, if it is not <see langword="null" />, otherwise an empty sequence of the same type.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemCanBeNull]
        public static IEnumerable EnumerateSafely([JetBrains.Annotations.CanBeNull] this IEnumerable sequence)
        {
            if(ReferenceEquals(sequence, null))
            {
                return Array.Empty<object>();
            }

            return sequence;
        }

        /// <summary>
        ///     Allows any sequence to be safely enumerated, even if it is in fact <see langword="null" />.
        /// </summary>
        /// <param name="sequence">
        ///     The sequence to enumerate.
        /// </param>
        /// <param name="enumerationKind">
        ///     The kind of the enumeration to perform.
        /// </param>
        /// <returns>
        ///     The sequence given, if it is not <see langword="null" />, otherwise an empty sequence of the same type.
        ///     <see langword="null" /> elements will not be emitted if <paramref name="enumerationKind" /> is <see cref="EnumerationKind.ExcludeNulls" />.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemCanBeNull]
        public static IEnumerable EnumerateSafely([JetBrains.Annotations.CanBeNull] this IEnumerable sequence, EnumerationKind enumerationKind)
        {
            if(ReferenceEquals(sequence, null))
            {
                yield break;
            }

            foreach(var item in sequence)
            {
                if(ReferenceEquals(item, null) && (enumerationKind == EnumerationKind.ExcludeNulls))
                {
                    continue;
                }

#pragma warning disable S2259
                yield return item;
#pragma warning restore S2259
            }
        }

        /// <summary>
        ///     Enumerates the children of an object recursively.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the parent and its children.
        /// </typeparam>
        /// <param name="parent">
        ///     The parent object to enumerate.
        /// </param>
        /// <param name="enumerate">
        ///     A function that enumerates the immediate children of a parent.
        /// </param>
        /// <returns>
        ///     A sequence containing the parent and all of its descendents enumerated in a depth-first search.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public static IEnumerable<T> EnumerateRecursively<T>([JetBrains.Annotations.CanBeNull] this T parent, [JetBrains.Annotations.NotNull] Func<T, IEnumerable<T>> enumerate) where T : class
        {
            enumerate.Validate(nameof(enumerate), ObjectIs.NotNull);

            if(ReferenceEquals(parent, null))
            {
                yield break;
            }

            yield return parent;

            var children = enumerate(parent);
            if(ReferenceEquals(children, null))
            {
                yield break;
            }

            foreach(var child in children)
            {
                if(ReferenceEquals(child, null))
                {
                    continue;
                }

                foreach(var x in EnumerateRecursively(child, enumerate))
                {
                    yield return x;
                }
            }
        }

        /// <summary>
        ///     Recursively searches the object model given and returns all the objects of type
        ///     <typeparamref name="TChild" /> found in properties and enumerations in the root object and its descendants.
        /// </summary>
        /// <param name="root">
        ///     The root of the object model to search.
        /// </param>
        /// <returns>
        ///     A sequence containing all the child objects found directly or indirectly the root object.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public static IEnumerable<TChild> GetChildren<TChild>([JetBrains.Annotations.CanBeNull] this object root) where TChild : class
        {
            return GetChildren<TChild>(root, null);
        }

        /// <summary>
        ///     Recursively searches the object model given and returns all the objects of type
        ///     <typeparamref name="TChild" /> found in properties and enumerations in the root object and its descendants.
        /// </summary>
        /// <param name="root">
        ///     The root of the object model to search.
        /// </param>
        /// <param name="parentType">
        ///     The type of the parent object (used to prevent uncontrolled recursion).
        /// </param>
        /// <returns>
        ///     A sequence containing all the child objects found directly or indirectly the root object.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        private static IEnumerable<TChild> GetChildren<TChild>([JetBrains.Annotations.CanBeNull] object root, [JetBrains.Annotations.CanBeNull] Type parentType) where TChild : class
        {
            if(ReferenceEquals(root, null))
            {
                yield break;
            }

            var type = root.GetType();

            // If the root object is a child return the root object.
            if(type == typeof(TChild))
            {
                yield return (TChild)root;

                // If the root object is a child then that is the end of the line.
                yield break;
            }

            foreach(var child in GetEnumerableChildren<TChild>(root, type))
            {
                yield return child;
            }

            // Now check every readable public property of the root object.
            foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if(!property.CanRead || property.PropertyType.IsPrimitive || property.PropertyType.IsEnum || (property.PropertyType == typeof(string)) ||
                   (property.GetIndexParameters().Length > 0))
                {
                    // If the property cannot be read or is of a primitive type (e.g. a string or an integer) then we
                    // know it cannot be a child or have a property that yields a child.
                    continue;
                }

                if(property.PropertyType == parentType)
                {
                    // Avoid recursion.
                    continue;
                }

                var value = property.GetValue(root);
                if(ReferenceEquals(value, root))
                {
                    // Avoid recursion.
                    continue;
                }

                // If the property might yield children then we must search it.
                foreach(var e in GetChildren<TChild>(value, type))
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        ///     Gets the enumerable children from the root object.
        /// </summary>
        /// <param name="root">
        ///     The root object to attempt to enumerate.
        /// </param>
        /// <param name="type">
        ///     The type of the root object.
        /// </param>
        /// <returns>
        ///     A sequence containing all the child objects found directly or indirectly in the enumerable children of
        ///     the root object.
        /// </returns>
        [JetBrains.Annotations.NotNull]
        private static IEnumerable<TChild> GetEnumerableChildren<TChild>([JetBrains.Annotations.NotNull] object root, [JetBrains.Annotations.CanBeNull] Type type) where TChild : class
        {
            // If the root object is a sequence of children returns the non-null members of the sequence.
            var childSequence = root as IEnumerable<TChild>;
            if(!ReferenceEquals(childSequence, null))
            {
                foreach(var e in childSequence.EnumerateSafely(EnumerationKind.ExcludeNulls))
                {
                    yield return e;
                }

                yield break;
            }

            // If the root object is a sequence of non-children returns the children found in the non-null members of
            // the sequence.
            var objectSequence = root as IEnumerable;
            if(ReferenceEquals(objectSequence, null))
            {
                yield break;
            }

            foreach(var o in objectSequence.EnumerateSafely(EnumerationKind.ExcludeNulls))
            {
                foreach(var e in GetChildren<TChild>(o, type))
                {
                    yield return e;
                }
            }
        }
    }
}