using System.Globalization;

namespace SFloat;

/// <summary>
/// A float value that is string-based.
/// </summary>
public struct SFloat {
    
    /// <summary>
    /// Creates a new SFloat from a string. The float automatically gets the radix of the string.
    /// </summary>
    /// <param name="value">The value of the float in string.</param>
    /// <param name="radix">
    /// The radix (base) to interpret the digits. The valid range is 2 to 36. Default is 10.
    /// </param>
    public SFloat(string value, int? radix = null) {
        radix ??= 10;
        if (radix < 2 || radix > 36) 
            throw new ArgumentOutOfRangeException(nameof(radix), "The radix must be in the range of 2 to 36.");
        _radix  = radix.Value;
        _digits = "";
        value   = value.ToUpperInvariant();
        
        // Parse digits.
        var digitUpperBound = _radix - 1;
        for (var i = 0; i < value.Length; i++) {
            if (i == 0 && value[i] == '-') {    // Check for negative sign.
                _isNegative = true;
                continue;
            }

            if (value[i] == '.') {              // Check for float point.
                if (_floatPointIndex != -1) 
                    throw new FormatException("The float contains multiple float points.");
                _floatPointIndex = _digits.Length - 1;
                continue;
            }

            var digitValue = GetDigitValue(value[i]);
            if (digitValue < 0 || digitValue > digitUpperBound)  // Check for invalid digit.
                throw new FormatException("The float contains invalid digits.");
            _digits += value[i];
        }
        
        // If no float point is found, set it to the end of the float.
        if (_floatPointIndex == -1) _floatPointIndex = _digits.Length - 1;
    }

    internal string _digits { get; init; }              // The digits of the float.
    internal int _floatPointIndex { get; init; } = -1;  // The index of the float point.
                                                        // The index means the position after the index of digit in _digits.
                                                        // fltPtrIdx = 3 for [5, 7, 3, 4, 2] represents 5734.2
    internal int _radix { get; init; }                  // The radix of the float.
    internal bool _isNegative { get; init; }            // Whether the float is negative.
    
    private static int GetDigitValue(char digit) {
        // Get the value of the digit. Maximum supported radix: 36.
        // Returns -1 if the digit is invalid.
        return digit switch {
            >= '0' and <= '9' => digit - '0',
            >= 'a' and <= 'z' => digit - 'a' + 10,
            >= 'A' and <= 'Z' => digit - 'A' + 10,
            _                 => -1
        };
    }
    
    private static char GetDigitChar(int value) {
        // Get the character of the digit. Maximum supported radix: 36.
        // Returns '\0' if the value is invalid.
        return value switch {
            >= 0 and <= 9 => (char) (value + '0'),
            >= 10 and <= 35 => (char) (value - 10 + 'A'),
            _ => '\0'
        };
    }

    private SFloat Clone(string? digits          = null,
                         int?    floatPointIndex = null,
                         int?    radix           = null,
                         bool?   isNegative      = null) {
        return new SFloat {
            _digits          = digits ?? _digits,
            _floatPointIndex = floatPointIndex ?? _floatPointIndex,
            _radix           = radix ?? _radix,
            _isNegative      = isNegative ?? _isNegative
        };
    }

    public static SFloat operator -(SFloat flt) {
        return flt.Clone(isNegative: !flt._isNegative);
    }

    public static SFloat operator +(SFloat flt1, SFloat flt2) {
        // Handle negative numbers. Two operands must be positive for addition.
        if (flt1._isNegative && !flt2._isNegative) return flt2 - -flt1;
        if (!flt1._isNegative && flt2._isNegative) return flt1 - -flt2;
        if (flt1._isNegative && flt2._isNegative) return -(-flt1 + -flt2);
        
        // Handle mismatched radix. Always convert to the radix of the first operand.
        if (flt1._radix != flt2._radix) flt2 = flt2.ToDecimal().ToRadix(flt1._radix);
        
        // Addition
        var result        = new List<char>();
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        var carry         = 0;
        for (var i = -maxFracLength; i < maxIntLength; i++) {
            var sum = GetDigitValue(flt1.GetDigitAt(i)) + GetDigitValue(flt2.GetDigitAt(i)) + carry;
            if (sum >= flt1._radix) {
                carry =  1;
                sum   -= flt1._radix;
            } else {
                carry = 0;
            }
            result.Insert(0, GetDigitChar(sum));
        }
        
        // Add the carry to the integer part (if any).
        if (carry == 1) {
            result.Insert(0, GetDigitChar(1));
            maxIntLength++;
        }

        return new SFloat {
            _digits          = new string(result.ToArray()),
            _floatPointIndex = maxIntLength - 1,
            _radix           = flt1._radix,
            _isNegative      = false
        };
    }
    
    public static SFloat operator +(SFloat flt1, decimal flt2) {
        return flt1 + new SFloat(flt2.ToString(CultureInfo.InvariantCulture));
    }
    
    public static SFloat operator +(decimal flt1, SFloat flt2) {
        return new SFloat(flt1.ToString(CultureInfo.InvariantCulture)) + flt2;
    }
    
    public static SFloat operator -(SFloat flt1, SFloat flt2) {
        // Handle negative numbers. flt1 must be greater than flt2 for subtraction.
        // Both operands must be positive.
        if (flt1 == flt2) return new SFloat("0", flt1._radix);
        if (!flt1._isNegative && flt2._isNegative) return flt1 + -flt2;
        if (flt1._isNegative && !flt2._isNegative) return -(-flt1 + flt2);
        if (flt1._isNegative && flt2._isNegative) {
            if (-flt1 < -flt2) return -flt2 - -flt1;
            if (-flt1 > -flt2) return -(-flt1 - -flt2);
        }
        if (!flt1._isNegative && !flt2._isNegative) {
            if (flt1 < flt2) return -(flt2 - flt1);
        }
        
        // Handle mismatched radix. Always convert to the radix of the first operand.
        if (flt1._radix != flt2._radix) flt2 = flt2.ToDecimal().ToRadix(flt1._radix);
        
        // Subtraction
        var result        = new List<char>();
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        var digits        = new int[maxIntLength + maxFracLength, 2];
        for (var i = -maxFracLength; i < maxIntLength; i++) {
            digits[i, 0] = GetDigitValue(flt1.GetDigitAt(i));
            digits[i, 1] = GetDigitValue(flt2.GetDigitAt(i));
        }

        for (var i = -maxFracLength; i < maxIntLength; i++) {
            var diff = digits[i, 0] - digits[i, 1];
            if (diff < 0) {
                diff += flt1._radix;
                digits[i + 1, 0]--;
            }
            result.Insert(0, GetDigitChar(diff));
        }
        
        return new SFloat {
            _digits          = new string(result.ToArray()),
            _floatPointIndex = maxIntLength - 1,
            _radix           = flt1._radix,
            _isNegative      = false
        };
    }
    
    public static bool operator >(SFloat flt1, SFloat flt2) {
        // Handle different signs.
        if (flt1._isNegative && !flt2._isNegative) return false;
        if (!flt1._isNegative && flt2._isNegative) return true;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1._radix != flt2._radix) {
            flt1 = flt1.ToDecimal();
            flt2 = flt2.ToDecimal();
        }
        
        // Compare digit by digit.
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        for (var i = maxIntLength - 1; i >= 0; i--) { // Integer part
            if (GetDigitValue(flt1.GetDigitAt(i)) < GetDigitValue(flt2.GetDigitAt(i))) return false;
        }

        for (var i = 1; i <= maxFracLength; i++) { // Fractional part
            if (GetDigitValue(flt1.GetDigitAt(-i)) < GetDigitValue(flt2.GetDigitAt(-i))) return false;
        }
        
        return true;
    }

    public static bool operator >=(SFloat flt1, SFloat flt2) {
        return flt1 > flt2 || flt1 == flt2;
    }

    public static bool operator ==(SFloat flt1, SFloat flt2) {
        // Handle different signs.
        if (flt1._isNegative != flt2._isNegative) return false;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1._radix != flt2._radix) {
            flt1 = flt1.ToDecimal();
            flt2 = flt2.ToDecimal();
        }
        
        // Compare digit by digit.
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        for (var i = -maxFracLength; i < maxIntLength; i++) {
            if (flt1.GetDigitAt(i) != flt2.GetDigitAt(i)) return false;
        }

        return true;
    }
    
    public static bool operator !=(SFloat flt1, SFloat flt2) {
        return !(flt1 == flt2);
    }
    
    public static bool operator <(SFloat flt1, SFloat flt2) {
        // Handle different signs.
        if (flt1._isNegative && !flt2._isNegative) return true;
        if (!flt1._isNegative && flt2._isNegative) return false;
        
        // Compare digit by digit.
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        for (var i = maxIntLength - 1; i >= 0; i--) { // Integer part
            if (GetDigitValue(flt1.GetDigitAt(i)) > GetDigitValue(flt2.GetDigitAt(i))) return false;
        }

        for (var i = 1; i <= maxFracLength; i++) { // Fractional part
            if (GetDigitValue(flt1.GetDigitAt(-i)) > GetDigitValue(flt2.GetDigitAt(-i))) return false;
        }
        
        return true;
    }
    
    public static bool operator <=(SFloat flt1, SFloat flt2) {
        return flt1 < flt2 || flt1 == flt2;
    }

    public static SFloat operator *(SFloat flt, int factor) {
        var result = flt;
        for (var i = 0; i < factor; i++) {
            result += flt;
        }

        return result;
    }

    public int FractionLength {
        get {
            if (_floatPointIndex == -1) return 0;
            return _digits.Length - _floatPointIndex - 1;
        }
    }
    
    public int IntegerLength {
        get {
            if (_floatPointIndex == -1) return _digits.Length;
            return _floatPointIndex + 1;
        }
    }

    private static readonly char[] zeroChar = new [] { '0' };

    /// <summary>
    /// Gets the digit at the specified index.
    /// </summary>
    /// <param name="index">
    /// 0 means the one's place (rightmost digit before the float point).
    /// -1 means the tenths place (leftmost digit after the float point).
    /// </param>
    /// <returns>
    /// The specified digit. Returns '0' if the index is out of range.
    /// </returns>
    public char GetDigitAt(int index) {
        // Integral part (index >= 0)
        if (index >= 0)
            return index >= IntegerLength ? '0' : _digits[IntegerLength - index - 1];
        
        // Fractional part (index < 0)
        return -index > FractionLength ? '0' : _digits[IntegerLength - index - 1];
    }

    public SFloat SetDigitAt(int index, int value) {
        if (value < 0 || value >= _radix)
            throw new ArgumentOutOfRangeException(nameof(value),
                "The value must be in the range of 0 to the maximally supported value by the radix.");
        var c = GetDigitChar(value);
        if (c == '\0')
            throw new ArgumentOutOfRangeException(nameof(value),
                "The value must be in the range of 0 to the maximally supported value by the radix.");

        var digits          = _digits.ToCharArray();
        var floatPointIndex = _floatPointIndex;

        switch (index) {
            // Integral part (index >= 0)
            case >= 0 when index < IntegerLength:
                digits[index - IntegerLength + 1] = c;
                break;
            // extend the integral part when needed
            case >= 0 when index >= IntegerLength: {
                while (index >= IntegerLength) {
                    digits = zeroChar.Concat(digits).ToArray();
                    floatPointIndex++;
                }
                return SetDigitAt(index, value);
            }
            // Fraction part (index < 0)
            case < 0 when -index <= FractionLength:
                digits[IntegerLength - index - 1] = c;
                break;
            // extend the fraction part when needed
            case < 0 when -index > FractionLength: {
                while (-index > FractionLength) {
                    digits = digits.Concat(zeroChar).ToArray();
                }
                return SetDigitAt(index, value);
            }
        }
        
        return Clone(digits: new string(digits), floatPointIndex: floatPointIndex);
    }

    public override string ToString() {
        // var chars = new char[_digits.Length + 2];
        // if (_isNegative) chars[0] = '-';
        // for (var i = 0; i < _digits.Length; i++) {
        //     if (i <= _floatPointIndex) chars[_isNegative ? i + 1 : i] = _digits[i];       // Before the float point.
        //     if (i == _floatPointIndex && FractionLength == 0) break;
        //     if (i == _floatPointIndex) chars[_isNegative ? i + 1 : i] = '.';             // At the float point.
        //     else chars[_isNegative ? i + 2 : i + 1]                        = _digits[i]; // After the float point.
        // }
        // return new string(chars).Trim('\0');

        var intChars = new char[IntegerLength];
        for (var i = IntegerLength - 1; i >= 0; i--) {
            intChars[IntegerLength - i - 1] = GetDigitAt(i);
        }
        if (FractionLength == 0)
            return $"{(_isNegative ? "-" : "")}{new string(intChars)}";
        
        var fracChars = new char[FractionLength];
        for (var i = 1; i <= FractionLength; i++) {
            fracChars[i - 1] = GetDigitAt(-i);
        }
        return $"{(_isNegative ? "-" : "")}{new string(intChars)}.{new string(fracChars)}";
    }
}