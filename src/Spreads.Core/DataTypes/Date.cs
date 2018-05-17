﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spreads.DataTypes
{
    /// <summary>
    /// Date stored as a number of days since zero.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public readonly struct Date
    {
        private readonly int _value;

        public Date(DateTime datetime)
        {
            _value = (int)(datetime.Ticks / TimeSpan.TicksPerDay);
        }

        public DateTime DateTime => (DateTime)this;

        public static explicit operator DateTime(Date date)
        {
            return new DateTime(date._value * TimeSpan.TicksPerDay, DateTimeKind.Unspecified);
        }

        public static explicit operator Date(DateTime datetime)
        {
            return new Date(datetime);
        }

        public override string ToString()
        {
            return ((DateTime)this).ToString("yyyy-MM-dd");
        }

        public string ToString(string format)
        {
            return ((DateTime)this).ToString(format);
        }
    }

    /// <summary>
    /// Time stored as number of milliseconds.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public struct Time
    {
        private readonly int _value;

        public Time(DateTime datetime)
        {
            _value = (int)((datetime.Ticks % TimeSpan.TicksPerDay) / TimeSpan.TicksPerMillisecond);
        }

        public Time(TimeSpan timespan)
        {
            _value = (int)(timespan.Ticks / TimeSpan.TicksPerMillisecond);
        }

        public TimeSpan TimeSpan => (TimeSpan)this;

        public static explicit operator TimeSpan(Time time)
        {
            return new TimeSpan(time._value * TimeSpan.TicksPerMillisecond);
        }

        public static explicit operator Time(TimeSpan timespan)
        {
            return new Time(timespan);
        }

        public override string ToString()
        {
            return ((TimeSpan)this).ToString("hh:mm:ss.fff");
        }

        public string ToString(string format)
        {
            return ((TimeSpan)this).ToString(format);
        }
    }
}