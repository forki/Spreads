﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using NUnit.Framework;
using Spreads.Collections;
using Spreads.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Spreads.Core.Tests.Cursors
{
    // TODO SCM debug.assert fails
    [Category("CI")]
    [TestFixture]
    public class AsyncCursorTests
    {
        [Test]
        public void DeadCursorDoesNotCauseEndlessLoopInNotifyUpdate()
        {
            var sm = new SortedMap<int, int>();

            sm.Add(1, 1);

            var cursor = sm.GetAsyncEnumerator();
            Assert.True(cursor.MoveNext());
            Assert.False(cursor.MoveNext());

            cursor.MoveNextAsync();

            cursor = default;

            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.Collect(2, GCCollectionMode.Forced, true);

            //var t = Task.Run(() => cursor.MoveNextAsync(cts.Token));

            sm.Add(2, 2);
            sm.Add(3, 3);

            Assert.True(sm.Count == 3);
        }

        [Test]
        public void CancelledCursorDoesntCauseEndlessLoopInNotifyUpdate()
        {
            var sm = new SortedMap<int, int>();

            sm.Add(1, 1);

            var cursor = sm.GetAsyncEnumerator();
            Assert.True(cursor.MoveNext());
            Assert.False(cursor.MoveNext());

            cursor.MoveNextAsync();

            sm.Add(2, 2);
            sm.Add(3, 3);

            Assert.True(sm.Count == 3);
        }

        //[Test]
        //public void CancelledCursorThrowsOperationCancelledException()
        //{
        //    var sm = new SortedMap<int, int>();

        //    sm.Add(1, 1);

        //    var cursor = sm.GetCursor();
        //    Assert.True(cursor.MoveNext());
        //    Assert.False(cursor.MoveNext());

        //    var cts = new CancellationTokenSource();

        //    var t = Task.Run(() =>
        //    {
        //        Thread.Sleep(100);
        //        var task = cursor.MoveNextAsync(cts.Token);
        //        Assert.True(task.IsCanceled);
        //        //task.Wait();
        //    });

        //    cts.Cancel();
        //    t.Wait();
        //}

        //[Test]
        //public void CouldCancelRangeCursor()
        //{
        //    var sm = new SortedMap<int, int>();

        //    sm.Add(-1, -1);
        //    sm.Add(1, 1);
        //    sm.Add(2, 2);

        //    var range = sm.Range(0, 2, true, false);

        //    Assert.AreEqual(1, range.First.Present.Value);

        //    var cursor = range.GetCursor();

        //    var source = cursor.Source;
        //    Assert.AreEqual(1, source.Count());

        //    Assert.True(cursor.MoveNext());
        //    Assert.False(cursor.MoveNext());

        //    var cts = new CancellationTokenSource();

        //    var t = Task.Run(() =>
        //    {
        //        Thread.Sleep(100);
        //        var task = cursor.MoveNextAsync(cts.Token);
        //        Assert.True(task.IsCanceled);
        //    });

        //    cts.Cancel();
        //    t.Wait();
        //}

        [Test]
        public void RangeCursorStopsBeforeEndKey()
        {
            var sm = new SortedMap<int, int>();

            sm.Add(1, 1);
            sm.Add(2, 2);
            sm.Add(3, 3);
            var range = sm.Range(int.MinValue, 2, true, true);

            //Assert.AreEqual(1, range.First.Value);

            var cursor = range.GetAsyncEnumerator();

            var source = cursor.Source;

            var cts = new CancellationTokenSource();

            var t = Task.Run(async () =>
            {
                var moved = await cursor.MoveNextAsync();
                Assert.True(moved);
                moved = await cursor.MoveNextAsync();
                Assert.True(moved);
                moved = await cursor.MoveNextAsync();
                Assert.False(moved);
                Assert.AreEqual(2, cursor.CurrentKey);
                Assert.AreEqual(2, cursor.CurrentValue);
            });

            t.Wait();
        }

        // TODO!
        [Test, Explicit("hangs")]
        public void RangeCursorMovesAfterAwating()
        {
            var sm = new SortedMap<int, int>();

            sm.Add(1, 1);

            var range = sm.Range(int.MinValue, 2, true, true);

            //Assert.AreEqual(1, range.First.Value);

            var cursor = range.GetAsyncEnumerator();

            var source = cursor.Source;

            var cts = new CancellationTokenSource();

            var t = Task.Run(async () =>
            {
                var moved = await cursor.MoveNextAsync();
                Assert.True(moved);
                moved = await cursor.MoveNextAsync();
                Assert.True(moved);
                moved = await cursor.MoveNextAsync();
                Assert.False(moved);
                Assert.AreEqual(2, cursor.CurrentKey);
                Assert.AreEqual(2, cursor.CurrentValue);
            });
            Thread.Sleep(100);
            sm.Add(2, 2);
            sm.Add(3, 3);
            t.Wait();
        }

        [Test]
        public async Task NotificationWorksWhenCursorIsNotWating()
        {
            var sm = new SortedMap<int, int>();

            sm.Add(1, 1);
            sm.Add(2, 2);
            // sm.Flush();

            var cursor = sm.GetAsyncEnumerator();
            Assert.True(await cursor.MoveNextAsync());
            Assert.True(await cursor.MoveNextAsync());

            Assert.IsTrue(cursor.MovePrevious());

            await sm.TryAdd(3, 3);

            await Task.Delay(250);

            Assert.AreEqual(1, cursor.CurrentKey);
            Assert.AreEqual(1, cursor.CurrentValue);
        }

        [Test]
        public async Task CouldCancelCursor()
        {
            var sm = new SortedMap<int, int>();

            var cursor = sm.GetAsyncEnumerator();

            var completable = cursor as IAsyncCompletable;

            var t = Task.Run(async () =>
            {
                try
                {
                    await cursor.MoveNextAsync();
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.True(true);
                }
            });

            Thread.Sleep(100);

            completable?.TryComplete(true);

            t.Wait();
        }

        [Test]
        public async Task CouldEnumerateSMInBatchMode()
        {
            var map = new SortedMap<int, int>();
            var count = 1_000_000;

            for (int i = 0; i < count; i++)
            {
                await map.TryAdd(i, i);
            }

#pragma warning disable HAA0401 // Possible allocation of reference type enumerator
            var ae = map.GetAsyncEnumerator();
#pragma warning restore HAA0401 // Possible allocation of reference type enumerator

            var t = Task.Run(async () =>
            {
                using (Benchmark.Run("SCM.AsyncEnumerator", count))
                {
                    var cnt = 0;
                    while (await ae.MoveNextAsync())
                    {
                        cnt++;
                    }

                    await ae.DisposeAsync();
                    Assert.AreEqual(count * 2, cnt);
                }

                Benchmark.Dump();
            });

            for (int i = count; i < count * 2; i++)
            {
                await map.TryAdd(i, i);
            }
            await map.Complete();

            t.Wait();
        }

        [Test]
        public async Task CouldEnumerateSMUsingCursor()
        {
            var map = new SortedMap<int, int>();
            var count = 1_000_000;

            for (int i = 0; i < count; i++)
            {
                await map.TryAdd(i, i);
            }

#pragma warning disable HAA0401 // Possible allocation of reference type enumerator
            var ae = map.GetAsyncEnumerator();
#pragma warning restore HAA0401 // Possible allocation of reference type enumerator

            var t = Task.Run(async () =>
            {
                using (Benchmark.Run("SCM.AsyncEnumerator", count))
                {
                    var cnt = 0;
                    while (await ae.MoveNextAsync())
                    {
                        cnt++;
                    }

                    await ae.DisposeAsync();
                    Assert.AreEqual(count * 2, cnt);
                }

                Benchmark.Dump();
            });

            for (int i = count; i < count * 2; i++)
            {
                await map.TryAdd(i, i);
            }
            await map.Complete();

            t.Wait();
        }

        [Test]
        public async Task CouldEnumerateSCMInBatchMode()
        {
            Settings.SCMDefaultChunkLength = Settings.SCMDefaultChunkLength * 4;
            var scm = new SortedMap<int, int>();
            var count = 1_000_000; // Settings.SCMDefaultChunkLength - 1;

            //for (int i = 0; i < count; i++)
            //{
            //    await scm.TryAdd(i, i);
            //}

            Console.WriteLine("Added first half");

#pragma warning disable HAA0401 // Possible allocation of reference type enumerator
            var ae = scm.GetAsyncEnumerator();
#pragma warning restore HAA0401 // Possible allocation of reference type enumerator

            var t = Task.Run(async () =>
            {
                using (Benchmark.Run("SCM.AsyncEnumerator", count))
                {
                    //try
                    //{
                    var cnt = count;
                    while (await ae.MoveNextAsync())
                    {
                        if (cnt != ae.Current.Key)
                        {
                            ThrowHelper.ThrowInvalidOperationException($"cnt {cnt} != ae.Current.Key {ae.Current.Key}");
                        }

                        cnt++;
                    }

                    await ae.DisposeAsync();
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine("EXCEPTION: " + ex.ToString());
                    //    Console.WriteLine("INNER: " + ex.InnerException?.ToString());
                    //    throw;
                    //}

                    // Assert.AreEqual(scm.Count, cnt);
                }

                Benchmark.Dump();
            });

            // Thread.Sleep(1000);

            for (int i = count; i < count * 2; i++)
            {
                await scm.TryAdd(i, i);
                //Thread.SpinWait(50);
            }

            // Thread.Sleep(2000);

            await scm.Complete();

            t.Wait();
        }

        [Test]
        public async Task CouldEnumerateSCMUsingCursor()
        {
            Settings.SCMDefaultChunkLength = Settings.SCMDefaultChunkLength * 4;
            var scm = new SortedMap<int, int>();
            var count = 1_000_000; // Settings.SCMDefaultChunkLength - 1;

            for (int i = 0; i < count; i++)
            {
                await scm.TryAdd(i, i);
            }

            Console.WriteLine("Added first half");

#pragma warning disable HAA0401 // Possible allocation of reference type enumerator
            var ae = scm.GetAsyncEnumerator();
#pragma warning restore HAA0401 // Possible allocation of reference type enumerator

            var t = Task.Run(async () =>
            {
                using (Benchmark.Run("SCM.AsyncEnumerator", count))
                {
                    try
                    {
                        var cnt = 0;
                        while (await ae.MoveNextAsync())
                        {
                            if (cnt != ae.Current.Key)
                            {
                                ThrowHelper.ThrowInvalidOperationException();
                            }

                            cnt++;
                        }

                        await ae.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EXCEPTION: " + ex.ToString());
                        throw;
                    }

                    // Assert.AreEqual(scm.Count, cnt);
                }

                Benchmark.Dump();
            });

            // Thread.Sleep(1000);

            for (int i = count; i < count * 2; i++)
            {
                await scm.TryAdd(i, i);
                //Thread.SpinWait(50);
            }

            // Thread.Sleep(2000);

            await scm.Complete();

            t.Wait();
        }

        [Test]
        public async Task CouldReadDataStreamWhileWritingFromManyThreads()
        {
            var map = new SortedMap<int, int>();

            var count = 1_000_000;
            var rounds = 5;

            var writeTask = Task.Run(async () =>
            {
                using (Benchmark.Run("Write", count * rounds, true))
                {
                    for (int j = 0; j < rounds; j++)
                    {
                        var t1 = Task.Run(async () =>
                        {
                            try
                            {
                                for (int i = j * count; i < (j + 1) * count; i++)
                                {
                                    await map.TryAddLast(i, i);
                                    Thread.SpinWait(10);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        });
                        await t1;
                    }
                }
            });

            AsyncCursor<int, int, SortedMapCursor<int, int>> cursor = null;
            var cnt = 0L;
            var readTask = Task.Run(async () =>
            {
                for (int r = 0; r < 1; r++)
                {
                    using (cursor = new AsyncCursor<int, int, SortedMapCursor<int, int>>(map.GetCursor()))
                    {
                        using (Benchmark.Run("Read", count * rounds, true))
                        {
                            try
                            {
                                while (await cursor.MoveNextAsync())
                                {
                                    Interlocked.Increment(ref cnt);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                            

                            // Left from coreclr 19161 tests, TODO remove when everything works OK
                            // here is a strong reference to cursor with side effects of printing to console
                            // Console.WriteLine("Last value: " + cursor.Current.Key);
                            // another strong reference after while loop, we dereference it's value and return from task
                            // lastKey1 = cursor.CurrentKey;
                        }
                    }
                }
            });


            

            var monitor = true;
            var t = Task.Run(async () =>
            {
                try
                {
                    while (monitor)
                    {
                        await Task.Delay(1000);
                        Console.WriteLine($"Key {cursor.CurrentKey}");
                        cursor.TryComplete(false);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });

            await writeTask;
            await map.Complete();
            map.NotifyUpdate(true);
            Console.WriteLine("Read after map complete:" + Interlocked.Read(ref cnt));
            await readTask;
            Console.WriteLine("Read after finish:" + Interlocked.Read(ref cnt));
            // Console.WriteLine("Last key: " + lastKey);

            Benchmark.Dump();

            map.Dispose();
            monitor = false;
        }
    }
}