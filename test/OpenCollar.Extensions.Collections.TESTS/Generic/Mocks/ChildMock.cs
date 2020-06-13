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

namespace OpenCollar.Extensions.Collections.TESTS.Generic.Mocks
{
    public class ChildMock : IEnumerable
    {
        private readonly ArrayList _targets = new ArrayList();

        public ChildMock()
        {
            for(var n = 0; n < 10; ++n)
            {
                _targets.Add(new EnumerableMock());
            }
        }

        public TargetMock Property1 { get; } = new TargetMock("ChildMock");

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="System.Collections.IEnumerator"> </see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _targets.GetEnumerator();
        }
    }
}