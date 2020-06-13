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

using OpenCollar.Extensions.Collections.Generic;

using Xunit;

namespace OpenCollar.Extensions.Collections.TESTS
{
    /// <summary>
    ///     A class for testing the <see cref="ListExtensions" /> class.
    /// </summary>
    // [TestClass]
    public class TestListExtensions
    {
        /// <summary>
        ///     Tests the <see cref="ListExtensions.Synchronize" /> method with custom equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeCustom()
        {
            // Basic
            var list1 = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            // Basic, reversed
            var list2 = new List<SyncTestClass> { list1[0], list1[1], list1[2], list1[3], list1[4] };

            // Shorter
            var list3 = new List<SyncTestClass> { new SyncTestClass(1), new SyncTestClass(2), new SyncTestClass(3) };

            // Longer
            var list4 = new List<SyncTestClass> { new SyncTestClass(6), new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            // Empty
            var list5 = new List<SyncTestClass>();

            CustomCompareTests(list1, list1, "Synchronizing a list with itself.");
            CustomCompareTests(new List<SyncTestClass>(list1), list1, "Synchronizing identical lists.");
            CustomCompareTests(new List<SyncTestClass>(list1), list2, "Synchronizing with reversed list.");
            CustomCompareTests(new List<SyncTestClass>(list1), list3, "Synchronizing with shorter list.");
            CustomCompareTests(new List<SyncTestClass>(list1), list4, "Synchronizing with longer list.");
            CustomCompareTests(new List<SyncTestClass>(list1), list5, "Synchronizing with empty list.");
            CustomCompareTests(new List<SyncTestClass>(list5), list1, "Synchronizing empty list with non-empty.");
        }

        /// <summary>
        ///     Tests the argument validation of the <see cref="ListExtensions.Synchronize{T}(IList{T},IList{T})" />
        ///     method with Custom equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeCustomValidation1()
        {
            var target = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };
            var source = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            Assert.Throws<ArgumentNullException>(() => target.Synchronize(null, (a, b) => a == b));
        }

        /// <summary>
        ///     Tests the argument validation of the <see cref="ListExtensions.Synchronize" /> method with Custom equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeCustomValidation2()
        {
            var target = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };
            var source = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            Assert.Throws<ArgumentNullException>(() => ListExtensions.Synchronize(null, source, (a, b) => a == b));
        }

        /// <summary>
        ///     Tests the argument validation of the <see cref="ListExtensions.Synchronize" /> method with Custom equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeCustomValidation3()
        {
            var target = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };
            var source = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            Assert.Throws<ArgumentNullException>(() => target.Synchronize(source, null));
        }

        /// <summary>
        ///     Tests the <see cref="ListExtensions.Synchronize" /> method with default equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeDefault()
        {
            // Basic
            var list1 = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            // Basic, reversed
            var list2 = new List<SyncTestClass> { list1[0], list1[1], list1[2], list1[3], list1[4] };

            // Shorter
            var list3 = new List<SyncTestClass> { new SyncTestClass(1), new SyncTestClass(2), new SyncTestClass(3) };

            // Longer
            var list4 = new List<SyncTestClass> { new SyncTestClass(6), new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            // Empty
            var list5 = new List<SyncTestClass>();

            DefaultCompareTests(list1, list1, "Synchronizing a list with itself.");
            DefaultCompareTests(new List<SyncTestClass>(list1), list1, "Synchronizing identical lists.");
            DefaultCompareTests(new List<SyncTestClass>(list1), list2, "Synchronizing with reversed list.");
            DefaultCompareTests(new List<SyncTestClass>(list1), list3, "Synchronizing with shorter list.");
            DefaultCompareTests(new List<SyncTestClass>(list1), list4, "Synchronizing with longer list.");
            DefaultCompareTests(new List<SyncTestClass>(list1), list5, "Synchronizing with empty list.");
            DefaultCompareTests(new List<SyncTestClass>(list5), list1, "Synchronizing empty list with non-empty.");
        }

        /// <summary>
        ///     Tests the argument validation of the <see cref="ListExtensions.Synchronize" /> method with default equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeDefaultValidation1()
        {
            var target = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };
            var source = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            Assert.Throws<ArgumentNullException>(() => target.Synchronize(null));
        }

        /// <summary>
        ///     Tests the argument validation of the <see cref="ListExtensions.Synchronize" /> method with default equality.
        /// </summary>
        [Fact]
        public void TestSynchronizeDefaultValidation2()
        {
            var target = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };
            var source = new List<SyncTestClass> { new SyncTestClass(5), new SyncTestClass(4), new SyncTestClass(3), new SyncTestClass(2), new SyncTestClass(1) };

            Assert.Throws<ArgumentNullException>(() => ListExtensions.Synchronize(null, source));
        }

        /// <summary>
        ///     Performs the tests on to lists synchronized using the Custom equality method.
        /// </summary>
        /// <param name="target">
        ///     The target.
        /// </param>
        /// <param name="source">
        ///     The source.
        /// </param>
        /// <param name="description">
        ///     The description.
        /// </param>
        private void CustomCompareTests([JetBrains.Annotations.NotNull] IList<SyncTestClass> target, [JetBrains.Annotations.NotNull] IList<SyncTestClass> source, string description)
        {
            var sourceClone = new List<SyncTestClass>(source);

            target.Synchronize(source, (a, b) => a == b);

            Assert.Equal(source.Count, target.Count);

            for(var n = 0; n < source.Count; ++n)
                Assert.Equal(source[n], target[n]);

            for(var n = 0; n < source.Count; ++n)
                Assert.True(ReferenceEquals(source[n], sourceClone[n]), "Item " + n + " of source list must match the equivalent in the original source list (the source list must be unchanged).  " + description);
        }

        /// <summary>
        ///     Performs the tests on to lists synchronized using the default equality method.
        /// </summary>
        /// <param name="target">
        ///     The target.
        /// </param>
        /// <param name="source">
        ///     The source.
        /// </param>
        /// <param name="description">
        ///     The description.
        /// </param>
        private void DefaultCompareTests([JetBrains.Annotations.NotNull] IList<SyncTestClass> target, [JetBrains.Annotations.NotNull] IList<SyncTestClass> source, string description)
        {
            var sourceClone = new List<SyncTestClass>(source);

            target.Synchronize(source);

            Assert.Equal(source.Count, target.Count);

            for(var n = 0; n < source.Count; ++n)
                Assert.Equal(source[n], target[n]);

            for(var n = 0; n < source.Count; ++n)
                Assert.True(ReferenceEquals(source[n], sourceClone[n]), "Item " + n + " of source list must match the equivalent in the original source list (the source list must be unchanged).  " + description);
        }
    }

    /// <summary>
    ///     An class used to represent values in the <see cref="TestListExtensions" /> tests.
    /// </summary>
    internal class SyncTestClass : IEquatable<SyncTestClass>
    {
        /// <summary>
        ///     The value represented by this object.
        /// </summary>
        private readonly int _value;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SyncTestClass" /> class.
        /// </summary>
        /// <param name="value">
        ///     The value represented by this object.
        /// </param>
        public SyncTestClass(int value)
        {
            _value = value;
        }

        /// <summary>
        ///     The value represented by this object.
        /// </summary>
        public int Value => _value;

        public static bool operator !=(SyncTestClass left, SyncTestClass right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(SyncTestClass left, SyncTestClass right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter;
        ///     otherwise, <see langword="false" />.
        /// </returns>
        /// <param name="other">
        ///     An object to compare with this object.
        /// </param>
        public bool Equals(SyncTestClass other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return _value == other._value;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" /> is equal to the current <see cref="System.Object" />.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if the specified <see cref="System.Object" /> is equal to the current
        ///     <see cref="System.Object" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <param name="obj">
        ///     The object to compare with the current object.
        /// </param>
        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            if(ReferenceEquals(this, obj))
                return true;
            if(obj.GetType() != GetType())
                return false;
            return Equals((SyncTestClass)obj);
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        ///     A hash code for the current <see cref="System.Object" />.
        /// </returns>
        public override int GetHashCode()
        {
            return _value;
        }
    }
}