﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using Spreads.Utils;
using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Spreads.Buffers
{
    public static class BufferPool<T>
    {
        // private static readonly ArrayPool<T> PoolImpl = new DefaultArrayPool<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Rent(int minLength)
        {
            return ArrayPool<T>.Shared.Rent(minLength);
        }

        /// <summary>
        /// Return an array to the pool.
        /// </summary>
        /// <param name="array">An array to return.</param>
        /// <param name="clearArray">Force clear of arrays of blittable types. Arrays that could have references are always cleared.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(T[] array, bool clearArray = false)
        {
            ArrayPool<T>.Shared.Return(array, clearArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static OwnedPooledArray<T> RentOwnedBuffer(int minLength, bool requireExact = true)
        {
            return ArrayMemoryPool<T>.Shared.RentCore(minLength);
        }
    }

    public static class BufferPool
    {
        // max pooled array size
        internal const int SharedBufferSize = 4096;

        internal const int StaticBufferSize = 16 * 1024;

        /// <summary>
        /// Shared buffers are for slicing of small PreservedBuffers
        /// </summary>
        [ThreadStatic]
        internal static OwnedPooledArray<byte> SharedBuffer;

        [ThreadStatic]
        internal static int SharedBufferOffset;

        /// <summary>
        /// Temp storage e.g. for serialization
        /// </summary>
        [ThreadStatic]
        internal static OwnedPooledArray<byte> ThreadStaticBuffer;

        /// <summary>
        /// Thread-static <see cref="OwnedPooledArray{T}"/> with size of <see cref="StaticBufferSize"/>.
        /// </summary>
        internal static OwnedPooledArray<byte> StaticBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!ThreadStaticBuffer.IsDisposed) { return ThreadStaticBuffer; }
                ThreadStaticBuffer = BufferPool<byte>.RentOwnedBuffer(StaticBufferSize);
                // Pin forever
                ThreadStaticBuffer.Pin();
                return ThreadStaticBuffer;
            }
        }

        /// <summary>
        /// Return a contiguous segment of memory backed by a pooled array
        /// </summary>
        /// <param name="length"></param>
        /// <param name="requireExact"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PreservedBuffer<byte> PreserveMemory(int length, bool requireExact = true) // TODO before it worked as if with true
        {
            // https://github.com/dotnet/corefx/blob/master/src/System.Buffers/src/System/Buffers/DefaultArrayPool.cs#L35
            // DefaultArrayPool has a minimum size of 16
            const int smallTreshhold = 16;
            if (length <= smallTreshhold)
            {
                if (SharedBuffer.IsDisposed)
                {
                    SharedBuffer = BufferPool<byte>.RentOwnedBuffer(SharedBufferSize, false);
                    // NB we must create a reference or the first PreservedBuffer could
                    // dispose _sharedBuffer on PreservedBuffer disposal.
                    SharedBuffer.Pin();
                    SharedBufferOffset = 0;
                }
                var bufferSize = SharedBuffer.Memory.Length;
                var newOffset = SharedBufferOffset + length;
                if (newOffset > bufferSize)
                {
                    // replace shared buffer, the old one will be disposed
                    // when all ReservedMemory views on it are disposed
                    var previous = SharedBuffer;
                    SharedBuffer = BufferPool<byte>.RentOwnedBuffer(SharedBufferSize, false);
                    SharedBuffer.Pin();
                    previous.Unpin();
                    SharedBufferOffset = 0;
                    newOffset = length;
                }
                var buffer = SharedBuffer.Memory.Slice(SharedBufferOffset, length);
                SharedBufferOffset = BitUtil.Align(newOffset, IntPtr.Size);
                return new PreservedBuffer<byte>(buffer);
            }
            // NB here we exclusively own the buffer and disposal of PreservedBuffer will cause
            // disposal and returning to pool of the ownedBuffer instance, unless references were added via
            // PreservedBuffer.Close() or PreservedBuffer.Memory.Reserve()/Pin() methods
            var ownedBuffer = BufferPool<byte>.RentOwnedBuffer(length, requireExact);
            //var buffer2 = ownedBuffer.Memory.Slice(0, length);
            return new PreservedBuffer<byte>(ownedBuffer.Memory);
        }

        internal static void DisposePreservedBuffers<T>(T[] array, int offset, int len)
        {
            // TODO it is possible to this without boxing using reflection,
            // however boxing is Gen0 here and should be fast
            for (int i = offset; i < offset + len; i++)
            {
                ((IDisposable)array[i]).Dispose();
            }
        }

        internal static bool IsPreservedBuffer<T>()
        {
            var ti = typeof(T).GetTypeInfo();
            if (ti.IsGenericTypeDefinition)
            {
                return ti.GetGenericTypeDefinition() == typeof(PreservedBuffer<>);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Use a thread-static buffer as a temporary placeholder. One must only call this method and use the returned value
        /// from a single thread (no async/await, etc.).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static OwnedPooledArray<byte> UseTempBuffer(int minimumSize)
        {
            if (minimumSize <= StaticBufferSize)
            {
                return StaticBuffer;
            }
            return BufferPool<byte>.RentOwnedBuffer(minimumSize);
        }
    }

    ///// <summary>
    ///// A memory pool that allows to get preserved buffers backed by pooled arrays.
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //public class PreservedBufferPool<T>
    //{
    //    [ThreadStatic]
    //    internal static OwnedMemory<T> _sharedBuffer;

    //    [ThreadStatic]
    //    // ReSharper disable once StaticMemberInGenericType
    //    internal static int _sharedBufferOffset;

    //    internal static int _sizeOfT = BinarySerializer.Size<T>();

    //    // max pooled array size
    //    internal int _sharedBufferSize;

    //    // https://github.com/dotnet/corefx/blob/master/src/System.Buffers/src/System/Buffers/DefaultArrayPool.cs#L35
    //    // DefaultArrayPool has a minimum size of 16
    //    internal int _smallTreshhold;

    //    /// <summary>
    //    /// Constructs a new PreservedBufferPool instance.
    //    /// Keep in mind that every thread using this pool will have a thread-static
    //    /// buffer of the size `sharedBufferSize * SizeOf(T)` for fast allocation
    //    /// of preserved buffers of size smaller or equal to smallTreshhold.
    //    /// </summary>
    //    /// <param name="sharedBufferSize">Size of thread-static buffers in number of T elements</param>
    //    public PreservedBufferPool(int sharedBufferSize = 0)
    //    {
    //        if (_sizeOfT <= 0)
    //        {
    //            throw new NotSupportedException("PreservedBufferPool only supports blittable types");
    //        }
    //        if (sharedBufferSize <= 0)
    //        {
    //            sharedBufferSize = 4096 / BinarySerializer.Size<T>();
    //        }
    //        else
    //        {
    //            var bytesLength = BitUtil.FindNextPositivePowerOfTwo(sharedBufferSize * _sizeOfT);
    //            sharedBufferSize = bytesLength / _sizeOfT;
    //        }
    //        _sharedBufferSize = sharedBufferSize;
    //        _smallTreshhold = 16;
    //    }

    //    /// <summary>
    //    /// Return a contiguous segment of memory backed by a pooled array
    //    /// </summary>
    //    /// <param name="length"></param>
    //    /// <returns></returns>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public PreservedBuffer<T> PreserveBuffer(int length)
    //    {
    //        if (length <= _smallTreshhold)
    //        {
    //            if (_sharedBuffer == null)
    //            {
    //                _sharedBuffer = BufferPool<T>.RentOwnedBuffer(_sharedBufferSize, false);
    //                // NB we must create a reference or the first PreservedBuffer could
    //                // dispose _sharedBuffer on PreservedBuffer disposal.
    //                _sharedBuffer.Retain();
    //                _sharedBufferOffset = 0;
    //            }
    //            var bufferSize = _sharedBuffer.Length;
    //            var newOffset = _sharedBufferOffset + length;
    //            if (newOffset > bufferSize)
    //            {
    //                // replace shared buffer, the old one will be disposed
    //                // when all ReservedMemory views on it are disposed
    //                var previous = _sharedBuffer;
    //                _sharedBuffer = BufferPool<T>.RentOwnedBuffer(_sharedBufferSize, false);
    //                _sharedBuffer.Retain();
    //                previous.Release();
    //                _sharedBufferOffset = 0;
    //                newOffset = length;
    //            }
    //            var buffer = _sharedBuffer.Memory.Slice(_sharedBufferOffset, length);

    //            _sharedBufferOffset = newOffset;
    //            return new PreservedBuffer<T>(buffer);
    //        }
    //        // NB here we exclusively own the buffer and disposal of PreservedBuffer will cause
    //        // disposal and returning to pool of the ownedBuffer instance, unless references were added via
    //        // PreservedBuffer.Close() or PreservedBuffer.Memory.Reserve()/Pin() methods
    //        var ownedBuffer = BufferPool<T>.RentOwnedBuffer(length, false);
    //        var buffer2 = ownedBuffer.Memory.Slice(0, length);
    //        return new PreservedBuffer<T>(buffer2);
    //    }
    //}
}