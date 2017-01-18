﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using NUnit.Framework;
using Spreads.Collections;

namespace Spreads.Tests {

    [TestFixture]
    public class VariantSeriesTest {

        [Test]
        public void TestTest() {
            Assert.True(true);
            System.Console.WriteLine("TestTest");
        }


        [Test]
        public void CouldReadVariantSeries() {

            var sm = new SortedMap<int, string>();
            for (int i = 0; i < 100; i++) {
                sm.Add(i, (i * 100).ToString());
            }

            var vs = new VariantSeries<int, string>(sm);

            foreach (var item in vs) {
                System.Console.WriteLine(item.Key.Get<int>() + ": " + item.Value.Get<string>());
            }

        }
    }
}
