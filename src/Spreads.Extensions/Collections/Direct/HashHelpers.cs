using System;
using System.Diagnostics.Contracts;

namespace Spreads.Collections.Direct
{
    internal static class HashHelpers {
        // Table of prime numbers to use as hash table sizes. 
        // A typical resize algorithm would pick the smallest prime number in this array
        // that is larger than twice the previous capacity. 
        // Suppose our Hashtable currently has capacity x and enough elements are added 
        // such that a resize needs to occur. Resizing first computes 2x then finds the 
        // first prime in the table greater than 2x, i.e. if primes are ordered 
        // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n. 
        // Doubling is important for preserving the asymptotic complexity of the 
        // hashtable operations such as add.  Having a prime guarantees that double 
        // hashing does not lead to infinite loops.  IE, your hash function will be 
        // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
        //public static readonly int[] primes = {
        //    3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        //    1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        //    17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        //    187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        //    1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369, 8639249, 10367101,
        //    12440537, 14928671, 17914409, 21497293, 25796759, 30956117, 37147349, 44576837, 53492207, 64190669,
        //    77028803, 92434613, 110921543, 133105859, 159727031, 191672443, 230006941, 276008387, 331210079,
        //    397452101, 476942527, 572331049, 686797261, 824156741, 988988137, 1186785773, 1424142949, 1708971541,
        //    2050765853, MaxPrimeArrayLength };

        //internal static readonly int[] primes = new int[] {
        //            3,
        //            7,
        //            13,
        //            31,
        //            61,
        //            127,
        //            251,
        //            509,
        //            1021,
        //            2039,
        //            4093,
        //            8191,
        //            16381,
        //            32749,
        //            65521,
        //            131071,
        //            262139,
        //            524287,
        //            1048573,
        //            2097143,
        //            4194301,
        //            8388593,
        //            16777213,
        //            33554393,
        //            67108859,
        //            134217689,
        //            268435399,
        //            536870909,
        //            1073741789,
        //            MaxPrimeArrayLength // 2147483647,
        //        };

        // every next is 2+ times larger than previous
        internal static readonly int[] primes = new int[]
        {
            5,
            11,
            23,
            53,
            113,
            251,
            509,
            1019,
            2039,
            4079,
            8179,
            16369,
            32749,
            65521,
            131063,
            262133,
            524269,
            1048571,
            2097143,
            4194287,
            8388587,
            16777183,
            33554393,
            67108837,
            134217689,
            268435399,
            536870879,
            1073741789,
            MaxPrimeArrayLength
        };

        //public static int GetPrime(int min) {
        //    if (min < 0)
        //        throw new ArgumentException("Arg_HTCapacityOverflow");
        //    Contract.EndContractBlock();

        //    for (int i = 0; i < primes.Length; i++) {
        //        int prime = primes[i];
        //        if (prime >= min) return prime;
        //    }

        //    return min;
        //}

        public static int GetGeneratoin(int min) {
            if (min < 0)
                throw new ArgumentException("Arg_HTCapacityOverflow");
            Contract.EndContractBlock();

            for (int i = 0; i < primes.Length; i++) {
                int prime = primes[i];
                if (prime >= min) return i;
            }
            return -1;
        }

        public static int GetMinPrime() {
            return primes[0];
        }

        // Returns size of hashtable to grow to.
        //public static int ExpandPrime(int oldSize) {
        //    int newSize = 2 * oldSize;

        //    // Allow the hashtables to grow to maximum possible size (~2G elements) before encoutering capacity overflow.
        //    // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        //    if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize) {
        //        Debug.Assert(MaxPrimeArrayLength == primes[GetGeneratoin(MaxPrimeArrayLength)], "Invalid MaxPrimeArrayLength");
        //        return MaxPrimeArrayLength;
        //    }

        //    return primes[GetGeneratoin(newSize)];
        //}


        // This is the maximum prime smaller than Array.MaxArrayLength
        public const int MaxPrimeArrayLength = 2147483629;

    }
}