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
    /// <param name="maxFractionLength">
    /// The maximum length of the fractional part. Default is <see cref="MAX_DEFAULT_FRAC_LENGTH"/>.
    /// The value must not be negative and must not exceed <see cref="MAX_SUPPORTED_FRAC_LENGTH"/>.
    /// </param>
    public SFloat(string value, int? radix = null, int? maxFractionLength = null) {
        radix ??= 10;
        if (radix < 2 || radix > 36) 
            throw new ArgumentOutOfRangeException(nameof(radix), "The radix must be in the range of 2 to 36.");
        maxFractionLength ??= MAX_DEFAULT_FRAC_LENGTH;
        if (maxFractionLength < 0 || maxFractionLength > MAX_SUPPORTED_FRAC_LENGTH)
            throw new ArgumentOutOfRangeException(nameof(maxFractionLength),
                $"The maximum fraction length must be in the range of 0 to {MAX_SUPPORTED_FRAC_LENGTH}.");
        Radix  = radix.Value;
        Digits = "";
        MaxFractionLength = maxFractionLength.Value;
        value   = value.ToUpperInvariant();
        
        // Parse digits.
        var digitUpperBound = Radix - 1;
        var fracDigitsCtr   = 0;
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
            if (FloatPointIndex == -1) continue;
            fracDigitsCtr++;
            if (fracDigitsCtr == MaxFractionLength) break;  // Truncate the fractional part if too long.
        }
        
        // If no float point is found, set it to the end of the float.
        if (FloatPointIndex == -1) FloatPointIndex = Digits.Length - 1;
        
        // Remove leading zeros of integer part.
        for (var i = 0; i <= FloatPointIndex; i++) {
            if (Digits[0] != '0' || FloatPointIndex == 0) break;
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
    internal int MaxFractionLength { get; init; }

    public const int MAX_SUPPORTED_FRAC_LENGTH = 1073741823;    // The maximum length of the fractional part.
    public const int MAX_DEFAULT_FRAC_LENGTH = 32;              // The default maximum length of the fractional part.
    
    internal static int GetDigitValue(char digit) {
        // Get the value of the digit. Maximum supported radix: 36.
        // Returns -1 if the digit is invalid.
        return digit switch {
            >= '0' and <= '9' => digit - '0',
            >= 'a' and <= 'z' => digit - 'a' + 10,
            >= 'A' and <= 'Z' => digit - 'A' + 10,
            _                 => -1
        };
    }
    
    internal static char GetDigitChar(int value) {
        // Get the character of the digit. Maximum supported radix: 36.
        // Returns '\0' if the value is invalid.
        return value switch {
            >= 0 and <= 9 => (char) (value + '0'),
            >= 10 and <= 35 => (char) (value - 10 + 'A'),
            _ => '\0'
        };
    }

    private SFloat Clone(string? digits            = null,
                         int?    floatPointIndex   = null,
                         int?    radix             = null,
                         bool?   isNegative        = null,
                         int?    maxFractionLength = null) {
        return new SFloat {
            Digits            = digits ?? Digits,
            FloatPointIndex   = floatPointIndex ?? FloatPointIndex,
            Radix             = radix ?? Radix,
            IsNegative        = isNegative ?? IsNegative,
            MaxFractionLength = maxFractionLength ?? MaxFractionLength
        };
    }

    public static readonly SFloat Zero = new ("0");

    public static bool operator true(SFloat flt) {
        return flt != Zero;
    }
    
    public static bool operator false(SFloat flt) {
        return flt == Zero;
    }

    public static SFloat operator -(SFloat flt) {
        return flt.Clone(isNegative: !flt.IsNegative);
    }

    public static SFloat operator +(SFloat flt1, SFloat flt2) {
        // Optimization: If one of the operands is zero, return the other operand.
        if (flt1 == Zero) return flt2;
        if (flt2 == Zero) return flt1;
        
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
            Digits            = new string(result.ToArray()),
            FloatPointIndex   = maxIntLength - 1,
            Radix             = flt1.Radix,
            IsNegative        = false,
            MaxFractionLength = Math.Max(flt1.MaxFractionLength, flt2.MaxFractionLength)
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
            Digits            = new string(result.ToArray()),
            FloatPointIndex   = maxIntLength - 1,
            Radix             = flt1.Radix,
            IsNegative        = false,
            MaxFractionLength = Math.Max(flt1.MaxFractionLength, flt2.MaxFractionLength)
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
        // Optimise for zero checking:
        //    If the Zero is on the right hand side, use a faster method without converting to decimal.
        //    Perform ordinary check if Zero is on the left hand side.
        if (flt2 is { IntegerLength: 1, FractionLength: 0 } && flt2.GetDigitAt(0) == '0')
            return flt1 is { IntegerLength: 1, FractionLength: 0 } && flt1.GetDigitAt(0) == '0';
        
        // Handle different signs.
        if (flt1.IsNegative != flt2.IsNegative) return false;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1.Radix != flt2.Radix) {
            flt1 = flt1.ToDecimal();
            flt2 = flt2.ToDecimal();
        }
        
        // If two operands have different lengths, they are not equal.
        if (flt1.Digits.Length != flt2.Digits.Length) return false;
        
        // Compare digit by digit.
        for (var i = -flt1.FractionLength; i < flt1.IntegerLength; i++) {
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

    public int FractionLength => Math.Min(Digits.Length - FloatPointIndex - 1, MaxFractionLength);

    public int IntegerLength => FloatPointIndex + 1;
    
    public bool IsInteger => FractionLength == 0;

    private static readonly char[] ZeroChar = ['0'];

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

    public char[] GetDigitsIn(int? start = null, int? end = null) {
        start ??= IntegerLength - 1;
        end  ??= -FractionLength;
        if (start < end) return [];
        var rtn = new char[start.Value - end.Value + 1];
        for (var i = start.Value; i >= end.Value; i--) {
            rtn[start.Value - i] = GetDigitAt(i);
        }
        return rtn;
    }

    public char[] GetIntegerDigits() {
        return GetDigitsIn(IntegerLength - 1, 0);
    }
    
    public char[] GetFractionalDigits() {
        return FractionLength == 0 ? [] : GetDigitsIn(-1, -FractionLength);
    }

    /// <summary>
    /// Sets the digit at the specified index. The operation is NOT in-place. A new SFloat is returned.
    /// </summary>
    /// <param name="index">The index of digit to be set.</param>
    /// <param name="value">The value of the digit to be set.</param>
    /// <returns>The new SFloat with the digit set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is out of range of 0 to the maximally supported value by the radix.
    /// </exception>
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
        
        int fracLength = FractionLength, intLength = IntegerLength;

        switch (index) {
            // Integral part (index >= 0)
            case >= 0 when index < intLength:
                digits[intLength - index - 1] = c;
                break;
            
            // extend the integral part when needed
            case >= 0 when index >= intLength: 
                while (index >= intLength) {
                    digits = ZeroChar.Concat(digits).ToArray();
                    intLength++;
                    floatPointIndex++;
                }

                digits[intLength - index - 1] = c;
                break;
            
            // Fraction part (index < 0)
            case < 0 when -index <= fracLength:
                digits[intLength - index - 1] = c;
                break;
            
            // extend the fraction part when needed
            case < 0 when -index > fracLength: 
                while (-index > fracLength) {
                    digits = digits.Concat(ZeroChar).ToArray();
                    fracLength++;
                }

                digits[intLength - index - 1] = c;
                break;
        }
        var str = new string(digits);
        if (floatPointIndex != str.Length - 1)
            str = str[..(floatPointIndex + 1)] + "." + str[(floatPointIndex + 1)..];
        if (IsNegative) str = "-" + str;
        return new SFloat(str, Radix);
    }
    
    /// <summary>
    /// Sets the digit at the specified index. The operation is NOT in-place. A new SFloat is returned.
    /// </summary>
    /// <param name="index">The index of digit to be set.</param>
    /// <param name="value">The value of the digit to be set.</param>
    /// <returns>The new SFloat with the digit set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the digit is out of range of 0 to the maximally supported value by the radix.
    /// </exception>
    public SFloat SetDigitAt(int index, char value) {
        if (GetDigitValue(value) == -1)
            throw new ArgumentOutOfRangeException(nameof(value), "The value must be a valid digit.");
        return SetDigitAt(index, GetDigitValue(value));
    }

    /// <summary>
    /// Extracts the digit at the specified index while pertains its positional value.
    /// For example, SFloat("123.45", 10).ExtractDigitAt(-2) returns "0.05".
    /// </summary>
    /// <param name="index">
    /// The index of the digit to be extracted. 0 means the one's place (rightmost digit before the float point).
    /// -1 means the tenths place (leftmost digit after the float point).
    /// </param>
    /// <returns>The SFloat representation of the extracted digit. 0 if index is out of range.</returns>
    public SFloat ExtractDigitAt(int index) {
        var rtn = Zero;
        return rtn.SetDigitAt(index, GetDigitValue(GetDigitAt(index)));
    }

    public static bool TryParse(string s, out SFloat result, int? radix = null) {
        radix ??= 10;
        try {
            result = new SFloat(s, radix.Value);
            return true;
        } catch {
            result = Zero;
            return false;
        }
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