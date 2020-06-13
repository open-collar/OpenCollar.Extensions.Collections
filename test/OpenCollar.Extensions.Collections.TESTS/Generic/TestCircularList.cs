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

using OpenCollar.Extensions.Collections.Generic;

using Xunit;

namespace OpenCollar.Extensions.Collections.TESTS.Generic
{
    // [TestClass]
    public class TestCircularList
    {
        [Fact]
        public void Add_Basic()
        {
            const int MAX = 10;
            const int SENTINEL = 999;
            var list = new CircularList<int>(MAX);

            // Make sure some basic facts are true after inserting items
            list.Add(SENTINEL);
            Assert.Equal(1, list.Count);
            Assert.Equal(SENTINEL, list[0]);
            Assert.True(list.Contains(SENTINEL));
        }

        [Fact]
        public void Add_Complex()
        {
            const int MAX = 10;
            const int SENTINEL = 999;
            var list = new CircularList<int>(MAX);

            // Make sure some basic facts are true after inserting items

            for(var n = 1; n <= MAX * 2; ++n)
            {
                var newValue = SENTINEL + (n - 1);

                list.Add(newValue);

                Assert.Equal(n <= MAX ? n : MAX, list.Count);
                Assert.Equal(newValue, list[(n <= MAX ? n : MAX) - 1]);
                Assert.True(list.Contains(newValue));
                if(n <= MAX)
                    Assert.True(list.Contains(SENTINEL), "List will contain sentinel before it has looped: " + n);
                else
                    Assert.False(list.Contains(SENTINEL), "List will not contain sentinel after it has looped: " + n);

                // Now check that the values are what we expect
                var lower = n <= MAX ? 1 : 1 + n - MAX;
                var max = n <= MAX ? n : MAX;

                for(var x = 1; x <= max; ++x)
                    Assert.Equal(SENTINEL + x + lower - 2, list[x - 1]);
            }
        }

        [Fact]
        public void CopyTo()
        {
            const int SENTINEL = 999;
            var shortArray = new[] { 1, 2, 3, 4, 5 };
            var list = new CircularList<int>(shortArray.Length, shortArray);

            // Let's make sure that the basic case works, so we should have an array containing the values 5-1. Check
            // the array contains the correct values.
            var testArray = new int[shortArray.Length];
            list.CopyTo(testArray, 0);
            var n = shortArray.Length - 1;
            foreach(var value in shortArray)
                Assert.Equal(value, testArray[n--]);

            // Now make sure it orders correctly
            list.Add(SENTINEL);

            // List should now contain 4-1, Sentinel
            list.CopyTo(testArray, 0);
            Assert.Equal(4, testArray[0]);
            Assert.Equal(SENTINEL, testArray[testArray.Length - 1]);

            // Let's make sure that the basic case works, so we should have an array containing the values 5-1. Check
            // the array contains the correct values.
            testArray = new int[shortArray.Length * 2];
            for(n = 0; n < testArray.Length; ++n)
                testArray[n] = SENTINEL;

            list.Load(shortArray);
            list.CopyTo(testArray, 1);
            Assert.Equal(SENTINEL, testArray[0]);

            n = shortArray.Length;
            foreach(var value in shortArray)
                Assert.Equal(value, testArray[n--]);

            for(n = shortArray.Length + 1; n < testArray.Length; ++n)
                Assert.Equal(SENTINEL, testArray[n]);
        }

        [Fact]
        public void Count()
        {
            const int MAX = 10;
            var list = new CircularList<int>(MAX);

            // Check that length increments as it should.
            for(var n = 1; n < MAX * 2; ++n)
            {
                list.Add(n);
                Assert.Equal(n <= MAX ? n : MAX, list.Count);
            }

            // Now let's reset and check that it all twangs back
            list.Clear();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void Instantiate_Basic()
        {
            const int MAX = 10;

            // Check we can actually create something and it is correctly instatiated
            var list = new CircularList<int>(MAX);
            Assert.NotNull(list);
            Assert.Equal(MAX, list.MaxLength);
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void Instantiate_Preload()
        {
            const int MAX = 10;

            // Now check the more complex case of instantiating a list with an existing set of values.
            var shortArray = new[] { 1, 2, 3 };
            var list = new CircularList<int>(MAX, shortArray);
            Assert.NotNull(list);
            Assert.Equal(MAX, list.MaxLength);
            Assert.Equal(shortArray.Length, list.Count);

            // Check the array contains the correct values.
            var n = shortArray.Length - 1;
            foreach(var value in list)
                Assert.Equal(shortArray[n--], value);
        }

        [Fact]
        public void Load()
        {
            // Now check the more complex case of loading a list with a set of values.
            const int SENTINEL = 999;
            var shortArray = new[] { 1, 2, 3, 4, 5 };
            var list = new CircularList<int>(shortArray.Length);

            list.Add(SENTINEL);
            list.Load(shortArray);
            Assert.NotNull(list);
            Assert.Equal(shortArray.Length, list.MaxLength);
            Assert.Equal(shortArray.Length, list.Count);

            // The sentinel should have been cleared when loaded, let's check
            Assert.False(list.Contains(SENTINEL));

            // Check the array contains the correct values.
            var n = shortArray.Length - 1;
            foreach(var value in list)
                Assert.Equal(shortArray[n--], value);
        }

        [Fact]
        public void Remove_Basic()
        {
            const int MAX = 10;
            const int SENTINEL = 999;
            var list = new CircularList<int>(MAX);

            // Make sure some basic facts are true after inserting items.
            list.Add(SENTINEL);
            Assert.Equal(1, list.Count);
            Assert.Equal(SENTINEL, list[0]);
            Assert.True(list.Contains(SENTINEL));

            // Make sure some basic facts are true after removing (by value).
            Assert.True(list.Remove(SENTINEL));
            Assert.Equal(0, list.Count);
            Assert.False(list.Contains(SENTINEL));

            // Make sure some basic facts are true after removing (by value).
            list.Add(SENTINEL);
            list.RemoveAt(0);
            Assert.Equal(0, list.Count);
            Assert.False(list.Contains(SENTINEL));
        }

        [Fact]
        public void Remove_Complex()
        {
            const int MAX = 10;
            const int SENTINEL = 999;
            var list = new CircularList<int>(MAX);

            // Initialize
            for(var n = 1; n <= MAX; ++n)
                list.Add(n);
            list.Add(SENTINEL);

            // This list should now contain 2, 3, 4, 5, 6, 7, 8, 9, 10, 999
            Assert.Equal(2, list[0]);
            Assert.Equal(SENTINEL, list[MAX - 1]);

            // Now remove something and check that things shuffle correctly.
            list.RemoveAt(0);
            Assert.Equal(MAX - 1, list.Count);
            Assert.Equal(3, list[0]);
            Assert.Equal(SENTINEL, list[MAX - 2]);

            // Now try removing by value rather than position/
            Assert.True(list.Remove(SENTINEL));
            Assert.Equal(MAX - 2, list.Count);
            Assert.Equal(3, list[0]);
            Assert.Equal(MAX, list[MAX - 3]);
            Assert.False(list.Contains(SENTINEL));

            // Now try upper boundary
            list.Clear();
            for(var n = 1; n <= MAX; ++n)
                list.Add(n);
            list.RemoveAt(MAX - 1);
            Assert.Equal(MAX - 1, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(MAX - 1, list[MAX - 2]);
        }

        [Fact]
        public void ToArray()
        {
            const int SENTINEL = 999;
            var shortArray = new[] { 1, 2, 3, 4, 5 };
            var list = new CircularList<int>(shortArray.Length, shortArray);

            // Let's make sure that the basic case works, so we should have an array containing the values 5-1. Check
            // the array contains the correct values.
            var testArray = list.ToArray();
            var n = shortArray.Length - 1;
            foreach(var value in shortArray)
                Assert.Equal(value, testArray[n--]);

            // Now make sure it orders correctly
            list.Add(SENTINEL);

            // List should now contain 4-1, Sentinel
            testArray = list.ToArray();
            Assert.Equal(4, testArray[0]);
            Assert.Equal(SENTINEL, testArray[testArray.Length - 1]);
        }
    }
}