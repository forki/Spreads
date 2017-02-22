﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spreads.Algorithms.Optimization {

    [StructLayout(LayoutKind.Sequential)]
    public struct Parameter : IEnumerable<double>, IEnumerator<double> {
        private readonly string _code;
        private readonly double _startValue;
        private readonly double _endValue;
        private readonly double _stepSize;
        private readonly int _steps;
        private readonly int _bigStepMultiple;
        private int _currentPosition;
        private int _offset;

        public Parameter(string code, double startValue, double endValue, double stepSize = 0, int bigStepMultiple = 1) {
            //
            if (endValue < startValue && stepSize > 0) { throw new ArgumentException("endValue <= startValue while step > 0"); }
            if (endValue > startValue && stepSize < 0) { throw new ArgumentException("endValue >= startValue while step < 0"); }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (stepSize == 0) {
                Trace.TraceWarning("Step size is zero, assuming differefe between start and end");
                stepSize = endValue - startValue;
            }
            _code = code;
            _startValue = startValue;
            _endValue = endValue;
            _stepSize = stepSize;
            //Debug.Assert((_endValue - _startValue) / _stepSize > 0);
            _steps = 1 + (int)Math.Ceiling((_endValue - _startValue) / _stepSize);
            if (bigStepMultiple < 1) { throw new ArgumentOutOfRangeException(nameof(bigStepMultiple)); }
            _bigStepMultiple = bigStepMultiple;
            _currentPosition = -1;
            _offset = 0;
        }

        public string Code => _code;
        public double StartValue => _startValue;
        public double EndValue => _endValue;
        public double StepSize => _stepSize;
        public int Steps => _steps;
        public int BigStepMultiple => _bigStepMultiple;

        public int CurrentPosition {
            get { return _currentPosition; }
            set {
                if (value < 0 || value >= _steps) throw new ArgumentOutOfRangeException(nameof(value));
                _currentPosition = value;
            }
        }

        public int GridPosition => _offset + (_currentPosition == -1 ? 0 : _currentPosition);

        public double this[int index] {
            get {
                if (index < 0) throw new ArgumentException("Parameter index is negative");
                if (index >= _steps) throw new ArgumentException("Parameter index is greater than number of steps");
                return index == (_steps - 1) ? _endValue : _startValue + index * _stepSize;
            }
        }

        /// <summary>
        /// Epsilon number of step around the position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public Parameter GetRegion(int position, int epsilon) {
            var offset = Math.Max(position - epsilon, 0);
            var start = this[offset];
            var end = this[Math.Min(position + epsilon, _steps - 1)];
            var newParameter = new Parameter(_code, start, end, _stepSize, _bigStepMultiple) {
                _offset = this._offset + offset
            };
            return newParameter;
        }

        public Parameter WithBigStep() {
            var newParameter = new Parameter(_code, _startValue, _endValue, _stepSize * _bigStepMultiple, 1) {
                _offset = this._offset
            };
            return newParameter;
        }

        public void Dispose() {
        }

        public double Current {
            get {
                Debug.Assert(_currentPosition >= 0, "Wrong access to Current of not started Parameter");
                Debug.Assert(_currentPosition < _steps, "Wrong _current position");
                return _currentPosition == (_steps - 1) ? _endValue : _startValue + _currentPosition * _stepSize;
            }
        }

        object IEnumerator.Current => this.Current;

        public Parameter GetEnumerator() {
            var copy = this;
            copy._currentPosition = -1;
            return copy;
        }

        public bool MoveNext() {
            var nextPosition = _currentPosition + 1;
            Debug.Assert(nextPosition <= _steps);
            if (nextPosition == _steps) { return false; }
            _currentPosition = nextPosition;
            return true;
        }

        public bool BigMoveNext() {
            Debug.Assert(_currentPosition + 1 <= _steps);
            if (_currentPosition + 1 == _steps) { return false; }
            if (_currentPosition == -1) {
                _currentPosition++;
                return true;
            }
            var nextPosition = _currentPosition + _bigStepMultiple;
            if (nextPosition >= _steps) {
                _currentPosition = _steps - 1;
                return true;
            }
            _currentPosition = nextPosition;
            return true;
        }

        public void Reset() {
            _currentPosition = -1;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator<double> IEnumerable<double>.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public static class ParameterExtensions {
        // useful to as a key to memoize target function result at a point, instead of int[]
        public static long LinearAddress(this Parameter[] parameterArray) {
            if (parameterArray == null) throw new ArgumentNullException(nameof(parameterArray));

            var address = -1L;
            if (parameterArray.Length == 0) return address;

            address = parameterArray[0].CurrentPosition;

            // previous * current dim + current addr
            // TODO test + review
            for (int i = 1; i < parameterArray.Length; i++) {
                address = address * parameterArray[i].Steps + parameterArray[i].GridPosition;
            }
            return address;
        }
    }
}