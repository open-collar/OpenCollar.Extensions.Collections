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
using System.Linq;

using JetBrains.Annotations;

using OpenCollar.Extensions.Collections.Generic;
using OpenCollar.Extensions.Collections.TESTS.Generic.Mocks;

using Xunit;

namespace OpenCollar.Extensions.Collections.TESTS.Generic
{
    public sealed class EnumeratorExtensionsTest
    {
        [Fact]
        public void TestEnumerateRecursively()
        {
            const int MAX = 10;

            var root = new RecursiveMock()
            {
                Id = 0
            };

            for(var n = 1; n < MAX; ++n)
            {
                var child = new RecursiveMock
                {
                    Id = n,
                    Child = root
                };
                root = child;
            }

            var children = root.EnumerateRecursively(r => new[] { r.Child });

            Assert.NotNull(children);

            var array = children.ToArray();

            Assert.Equal(MAX, array.Length);

            for(var n = 0; n < MAX; ++n)
            {
                Assert.Equal(MAX - 1 - n, array[n].Id);
            }

            children = root.EnumerateRecursively(r => null);
            Assert.NotNull(children);
            Assert.Single(children);
        }

        [Fact]
        public void TestEnumerateRecursivelyNull()
        {
            var children = ((RecursiveMock)null).EnumerateRecursively(r => new[] { r.Child });

            Assert.NotNull(children);
            Assert.Empty(children);
        }

        [Fact]
        public void TestEnumerateSafely()
        {
            var data = new string[] { "1", "2", "3" };
            var count = GetContents(((IEnumerable)data).EnumerateSafely()).Count();
            Assert.Equal(data.Length, count);

            data = new string[0];
            count = GetContents(((IEnumerable)data).EnumerateSafely()).Count();
            Assert.Equal(data.Length, count);

            data = null;
            count = GetContents(((IEnumerable)data).EnumerateSafely()).Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void TestEnumerateSafely_WithArg()
        {
            var data = new string[] { "1", "2", "3" };
            var count = GetContents(((IEnumerable)data).EnumerateSafely(EnumerationKind.IncludeNulls)).Count();
            Assert.Equal(data.Length, count);

            data = new string[] { "1", "2", "3", null };
            count = GetContents(((IEnumerable)data).EnumerateSafely(EnumerationKind.IncludeNulls)).Count();
            Assert.Equal(data.Length, count);

            data = new string[] { "1", "2", "3", null };
            count = GetContents(((IEnumerable)data).EnumerateSafely(EnumerationKind.ExcludeNulls)).Count();
            Assert.Equal(data.Length - 1, count);

            data = new string[0];
            count = GetContents(((IEnumerable)data).EnumerateSafely(EnumerationKind.IncludeNulls)).Count();
            Assert.Equal(data.Length, count);

            data = null;
            count = GetContents(((IEnumerable)data).EnumerateSafely(EnumerationKind.IncludeNulls)).Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void TestGenericEnumerateSafely()
        {
            var data = new string[] { "1", "2", "3" };
            var count = data.EnumerateSafely().Count();
            Assert.Equal(data.Length, count);

            data = new string[0];
            count = data.EnumerateSafely().Count();
            Assert.Equal(data.Length, count);

            data = null;
            count = data.EnumerateSafely().Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void TestGenericEnumerateSafely_WithArg()
        {
            var data = new string[] { "1", "2", "3" };
            var count = data.EnumerateSafely(EnumerationKind.IncludeNulls).Count();
            Assert.Equal(data.Length, count);

            data = new string[] { "1", "2", "3", null };
            count = data.EnumerateSafely(EnumerationKind.IncludeNulls).Count();
            Assert.Equal(data.Length, count);

            data = new string[] { "1", "2", "3", null };
            count = data.EnumerateSafely(EnumerationKind.ExcludeNulls).Count();
            Assert.Equal(data.Length - 1, count);

            data = new string[0];
            count = data.EnumerateSafely(EnumerationKind.IncludeNulls).Count();
            Assert.Equal(data.Length, count);

            data = null;
            count = data.EnumerateSafely(EnumerationKind.IncludeNulls).Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void TestGetChildren()
        {
            var children1 = ((object)null).GetChildren<TargetMock>();

            Assert.NotNull(children1);
            Assert.Empty(children1);

            var root = new RootMock();

            var children2 = root.GetChildren<TargetMock>();

            Assert.NotNull(children2);

            var array = children2.ToArray();

            Assert.Equal(304, array.Length);

            var groups = array.GroupBy(t => t.Source).ToDictionary(g => g.Key);

            Assert.Equal(3, groups.Count);

            Assert.Contains("RootMock", groups.Keys);
            Assert.Contains("ChildMock", groups.Keys);
            Assert.Contains("EnumerableMock", groups.Keys);
        }

        [JetBrains.Annotations.NotNull]
        [JetBrains.Annotations.CanBeNull]
        private static object[] GetContents([JetBrains.Annotations.NotNull][JetBrains.Annotations.CanBeNull] IEnumerable sequence)
        {
            var list = new ArrayList();
            foreach(var element in sequence)
            {
                list.Add(element);
            }

            return list.ToArray();
        }
    }
}