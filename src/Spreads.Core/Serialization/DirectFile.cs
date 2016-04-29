﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Spreads.Serialization {

    public struct DirectFile : IDisposable {
        private static long _counter;
        private static readonly object SyncRoot = new object();
        private readonly string _filename;
        internal long _capacity;

        private MemoryMappedFile _mmf;
        private FileStream _fileStream;
        internal DirectBuffer _buffer;


        public DirectFile(string filename, long minCapacity) {
            _filename = filename;
            _capacity = 0;
            _mmf = null;
            _fileStream = null;
            _buffer = default(DirectBuffer);
            Grow(minCapacity);
        }

        public void Grow(long minCapacity) {
            lock (SyncRoot) {
                _fileStream?.Dispose();
                _fileStream = new FileStream(_filename, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.ReadWrite, 4096,
                    FileOptions.RandomAccess);

                // NB another thread could have increase the map size and _capacity could be stale
                var bytesCapacity = Math.Max(_fileStream.Length, minCapacity);

                var sec = new MemoryMappedFileSecurity();
                _mmf?.Dispose();
                var unique = ((long)Process.GetCurrentProcess().Id << 32) | _counter++;
                // NB: map name must be unique
                var mmf = MemoryMappedFile.CreateFromFile(_fileStream,
                    $@"{Path.GetFileName(_filename)}.{unique}", bytesCapacity,
                    MemoryMappedFileAccess.ReadWrite, sec, HandleInheritability.Inheritable,
                    false);
                _mmf = mmf;

                unsafe
                {
                    byte* ptr = (byte*)0;
                    var va = _mmf.CreateViewAccessor(0, bytesCapacity, MemoryMappedFileAccess.ReadWrite);
                    var sh = va.SafeMemoryMappedViewHandle;
                    sh.AcquirePointer(ref ptr);
                    var ptrV = new IntPtr(ptr);
                    _buffer = new DirectBuffer(bytesCapacity, ptrV);
                    va.Dispose();
                }
                _capacity = bytesCapacity;
            }
        }

        public void Dispose() {
            _mmf.Dispose();
            _fileStream.Close();
        }

        public long Capacity => _capacity;
        public DirectBuffer Buffer => _buffer;
    }
}
