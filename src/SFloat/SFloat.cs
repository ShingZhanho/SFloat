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
        Radix  = radix.Value;
        Digits = "";
        value   = value.ToUpperInvariant();
        
        // Parse digits.
        var digitUpperBound = Radix - 1;
        for (var i = 0; i < value.Length; i++) {
            if (i == 0 && value[i] == '-') {    // Check for negative sign.
                IsNegative = true;
                continue;
            }

            if (value[i] == '.') {              // Check for float point.
                if (FloatPointIndex != -1) 
                    throw new FormatException("The float contains multiple float points.");
                FloatPointIndex = Digits.Length - 1;
                continue;
            }

            var digitValue = GetDigitValue(value[i]);
            if (digitValue < 0 || digitValue > digitUpperBound)  // Check for invalid digit.
                throw new FormatException("The float contains invalid digits.");
            Digits += value[i];
        }
        
        // If no float point is found, set it to the end of the float.
        if (FloatPointIndex == -1) FloatPointIndex = Digits.Length - 1;
        
        // Remove leading zeros of integer part.
        for (var i = 0; i < IntegerLength; i++) {
            if (Digits[i] != '0' || IntegerLength == 1) break;
            Digits = Digits[1..];
            FloatPointIndex--;
        }
        
        // Remove trailing zeros at fractional part.
        for (var i = Digits.Length - 1; i > FloatPointIndex; i--) {
            if (Digits[i] != '0') break;
            Digits = Digits[..^1];
        }
        
        // If the float is zero, set it to positive.
        if (Digits == "0") IsNegative = false;
    }
    
    public static implicit operator SFloat(string s) => new (s);

    internal string Digits { get; init; }              // The digits of the float.
    internal int FloatPointIndex { get; init; } = -1;  // The index of the float point.
                                                        // The index means the position after the index of digit in _digits.
                                                        // fltPtrIdx = 3 for [5, 7, 3, 4, 2] represents 5734.2
    internal int Radix { get; init; }                  // The radix of the float.
    internal bool IsNegative { get; init; }            // Whether the float is negative.
    
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
            Digits          = digits ?? Digits,
            FloatPointIndex = floatPointIndex ?? FloatPointIndex,
            Radix           = radix ?? Radix,
            IsNegative      = isNegative ?? IsNegative
        };
    }
    
    public static readonly SFloat Zero = new SFloat("0");

    public static SFloat operator -(SFloat flt) {
        return flt.Clone(isNegative: !flt.IsNegative);
    }

    public static SFloat operator +(SFloat flt1, SFloat flt2) {
        // Optimization: If one of the operands is zero, return the other operand.
        if (flt1.Digits == "0") return flt2;
        if (flt2.Digits == "0") return flt1;
        
        // Handle negative numbers. Two operands must be positive for addition.
        if (flt1.IsNegative && !flt2.IsNegative) return flt2 - -flt1;
        if (!flt1.IsNegative && flt2.IsNegative) return flt1 - -flt2;
        if (flt1.IsNegative && flt2.IsNegative) return -(-flt1 + -flt2);
        
        // Handle mismatched radix. Always convert to the radix of the first operand.
        if (flt1.Radix != flt2.Radix) flt2 = flt2.ToDecimal().ToRadix(flt1.Radix);
        
        // Addition
        var result        = new List<char>();
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        var carry         = 0;
        for (var i = -maxFracLength; i < maxIntLength; i++) {
            var sum = GetDigitValue(flt1.GetDigitAt(i)) + GetDigitValue(flt2.GetDigitAt(i)) + carry;
            if (sum >= flt1.Radix) {
                carry =  1;
                sum   -= flt1.Radix;
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
            Digits          = new string(result.ToArray()),
            FloatPointIndex = maxIntLength - 1,
            Radix           = flt1.Radix,
            IsNegative      = false
        };
    }
    
    public static SFloat operator +(SFloat flt1, decimal flt2) {
        return flt1 + new SFloat(flt2.ToString(CultureInfo.InvariantCulture));
    }
    
    public static SFloat operator +(decimal flt1, SFloat flt2) {
        return new SFloat(flt1.ToString(CultureInfo.InvariantCulture)) + flt2;
    }
    
    public static SFloat operator -(SFloat flt1, SFloat flt2) {
        // Optimisation: if the first operand is zero, return the negation of the second operand.
        if (flt1.Digits == "0") return -flt2;
        // if the second operand is zero, return the first operand.
        if (flt2.Digits == "0") return flt1;
        
        // Handle negative numbers. flt1 must be greater than flt2 for subtraction.
        // Both operands must be positive.
        if (flt1 == flt2) return new SFloat("0", flt1.Radix);
        if (!flt1.IsNegative && flt2.IsNegative) return flt1 + -flt2;
        if (flt1.IsNegative && !flt2.IsNegative) return -(-flt1 + flt2);
        if (flt1.IsNegative && flt2.IsNegative) {
            if (-flt1 < -flt2) return -flt2 - -flt1;
            if (-flt1 > -flt2) return -(-flt1 - -flt2);
        }
        if (!flt1.IsNegative && !flt2.IsNegative) {
            if (flt1 < flt2) return -(flt2 - flt1);
        }
        
        // Handle mismatched radix. Always convert to the radix of the first operand.
        if (flt1.Radix != flt2.Radix) flt2 = flt2.ToDecimal().ToRadix(flt1.Radix);
        
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
                diff += flt1.Radix;
                digits[i + 1, 0]--;
            }
            result.Insert(0, GetDigitChar(diff));
        }
        
        return new SFloat {
            Digits          = new string(result.ToArray()),
            FloatPointIndex = maxIntLength - 1,
            Radix           = flt1.Radix,
            IsNegative      = false
        };
    }
    
    public static bool operator >(SFloat flt1, SFloat flt2) {
        // Handle different signs.
        if (flt1.IsNegative && !flt2.IsNegative) return false;
        if (!flt1.IsNegative && flt2.IsNegative) return true;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1.Radix != flt2.Radix) {
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
        if (flt1.IsNegative != flt2.IsNegative) return false;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1.Radix != flt2.Radix) {
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
        if (flt1.IsNegative && !flt2.IsNegative) return true;
        if (!flt1.IsNegative && flt2.IsNegative) return false;
        
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
    
    public static SFloat operator *(int factor, SFloat flt) {
        return flt * factor;
    }

    public int FractionLength {
        get {
            if (FloatPointIndex == -1) return 0;
            return Digits.Length - FloatPointIndex - 1;
        }
    }
    
    public int IntegerLength {
        get {
            if (FloatPointIndex == -1) return Digits.Length;
            return FloatPointIndex + 1;
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
            return index >= IntegerLength ? '0' : Digits[IntegerLength - index - 1];
        
        // Fractional part (index < 0)
        return -index > FractionLength ? '0' : Digits[IntegerLength - index - 1];
    }

    public SFloat SetDigitAt(int index, int value) {
        if (value < 0 || value >= Radix)
            throw new ArgumentOutOfRangeException(nameof(value),
                "The value must be in the range of 0 to the maximally supported value by the radix.");
        var c = GetDigitChar(value);
        if (c == '\0')
            throw new ArgumentOutOfRangeException(nameof(value),
                "The value must be in the range of 0 to the maximally supported value by the radix.");

        var digits          = Digits.ToCharArray();
        var floatPointIndex = FloatPointIndex;

        switch (index) {
            // Integral part (index >= 0)
            case >= 0 when index < IntegerLength:
                digits[IntegerLength - index - 1] = c;
                break;
            // extend the integral part when needed
            case >= 0 when index >= IntegerLength: {
                while (index >= IntegerLength) {
                    digits = zeroChar.Concat(digits).ToArray();
                    floatPointIndex++;
                }
                return new SFloat(new string(new[] { c }.Concat(digits).ToArray()), Radix);
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
                return new SFloat(new string(digits.Concat([c]).ToArray()), Radix);
            }
        }
        
        return Clone(digits: new string(digits), floatPointIndex: floatPointIndex);
    }

    public override string ToString() {
        var intChars = new char[IntegerLength];
        for (var i = IntegerLength - 1; i >= 0; i--) {
            intChars[IntegerLength - i - 1] = GetDigitAt(i);
        }
        if (FractionLength == 0)
            return $"{(IsNegative ? "-" : "")}{new string(intChars)}";
        
        var fracChars = new char[FractionLength];
        for (var i = 1; i <= FractionLength; i++) {
            fracChars[i - 1] = GetDigitAt(-i);
        }
        return $"{(IsNegative ? "-" : "")}{new string(intChars)}.{new string(fracChars)}";
    }
}