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

using System.Collections.Generic;

namespace OpenCollar.Extensions.Collections.TESTS.Generic.Mocks
{
    public class RootMock
    {
        private TargetMock _property8 = new TargetMock("RootMock-UNFINDABLE-2");

        public string Property1 { get; set; }

        public string Property2 { get; set; }

        public DeadEndMock Property3 { get; } = new DeadEndMock();

        public IEnumerable<ChildMock> Property4 { get; } = new[] { new ChildMock(), new ChildMock() };

        public ChildMock Property5 { get; } = new ChildMock();

        public TargetMock Property6 { get; } = new TargetMock("RootMock");

        internal TargetMock Property7 { get; } = new TargetMock("RootMock-UNFINDABLE-1");

#pragma warning disable S2376 // Write-only properties should not be used

        public TargetMock Property8
#pragma warning restore S2376 // Write-only properties should not be used
        {
            set => _property8 = value;
        }

        private TargetMock Property9 { get; } = new TargetMock("RootMock-UNFINDABLE-3");

        public RootMock Property10 { get; } = null;
    }
}