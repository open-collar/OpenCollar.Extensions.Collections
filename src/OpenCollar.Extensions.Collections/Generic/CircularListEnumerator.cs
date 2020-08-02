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

using System.Collections;
using System.Collections.Generic;

namespace OpenCollar.Extensions.Collections.Generic
{
    /// <summary>
    ///     An enumerator for circular arrays.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of value the list will hold.
    /// </typeparam>
    internal class CircularListEnumerator<T> : Disposable, IEnumerator<T>
    {
        /// <summary>
        ///     The data across which we will iterate.
        /// </summary>
        private readonly T[] _data;

        /// <summary>
        ///     The length of the list.
        /// </summary>
        private readonly int _length;

        /// <summary>
        ///     The current item.
        /// </summary>
        private int _pointer = -1;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CircularListEnumerator&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="list">
        ///     The list across which to iterate.
        /// </param>
        internal CircularListEnumerator([JetBrains.Annotations.NotNull] CircularList<T> list)
        {
            _length = list.Count;
            _data = new T[_length];

            // NB. This organizes the data into the correct order - it's not just a straight copy of the internal data list.
            list.CopyTo(_data, 0);
        }

        /// <summary>
        ///     Custom finaliser.
        /// </summary>
        ~CircularListEnumerator()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        ///     The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current => _data[_pointer];

        /// <summary>
        ///     Gets the current element in the collection.
        /// </summary>
        /// <returns>
        ///     The current element in the collection.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     The enumerator is positioned before the first element of the collection or after the last element.
        /// </exception>
        /// <filterpriority>
        ///     2
        /// </filterpriority>
        [JetBrains.Annotations.NotNull]
        object IEnumerator.Current => Current;

        /// <summary>
        ///     Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if the enumerator was successfully advanced to the next element;
        ///     <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     The collection was modified after the enumerator was created.
        /// </exception>
        /// <filterpriority>
        ///     2
        /// </filterpriority>
        public bool MoveNext()
        {
            return ++_pointer < _length;
        }

        /// <summary>
        ///     Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     The collection was modified after the enumerator was created.
        /// </exception>
        /// <filterpriority>
        ///     2
        /// </filterpriority>
        public void Reset()
        {
            _pointer = -1;
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
            // clean up managed resources.
            if(disposing)
                for(var n = 0; n < _length; ++n)
                    _data[n] = default(T);

            base.Dispose(disposing);
        }
    }
}