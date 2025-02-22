namespace JacobS.SFloat;

/// <summary>
/// A float value that is string-based.
/// </summary>
public readonly partial struct SFloat : IEquatable<SFloat> {
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
        var temp = IntegerLength;
        for (var i = 0; i < temp; i++) {
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

    internal string Digits { get; init; }              // The digits of the float.
    internal int FloatPointIndex { get; init; } = -1;  // The index of the float point.
                                                        // The index means the position after the index of digit in _digits.
                                                        // fltPtrIdx = 3 for [5, 7, 3, 4, 2] represents 5734.2
    internal int Radix { get; init; }                  // The radix of the float.
    internal bool IsNegative { get; init; }            // Whether the float is negative.
    internal int MaxFractionLength { get; init; }

    public const int MAX_SUPPORTED_FRAC_LENGTH = 1073741823;    // The maximum length of the fractional part.
    public const int MAX_DEFAULT_FRAC_LENGTH = 128;             // The default maximum length of the fractional part.
    
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

    internal static char[] GetBitsFromValue(int value, int radix) {
        // Get the binary bits of the value.
        // Returns an empty array if the value is invalid.
        if (value < 0 || value >= radix) return [];
        var noOfBits = (int) Math.Ceiling(Math.Log2(radix)); // Radix must be a power of two. Not checked for performance.
        return Convert.ToString(value, 2).PadLeft(noOfBits, '0').ToCharArray();
    }

    internal static int GetValueFromBits(char[] bits) {
        // Gets the value from the binary bits.
        // Returns -1 if the value represented are not from 0 to 35.
        return Convert.ToInt32(new string(bits), 2);
    }

    /// <summary>
    /// Clones this SFloat with the specified properties. Other properties are copied from this SFloat.
    /// ***!! WARNING !!*** This method bypasses the truncation of leading and trailing zeros.
    /// This may break some functionalities.
    /// If you are to specify the digits, the leading and trailing zeros must be truncated manually.
    /// </summary>
    /// <remarks>
    /// Use <see cref="EnsureZeroTruncation"/> to ensure the cloned SFloat has proper truncations.
    /// </remarks>
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

    internal SFloat EnsureZeroTruncation() {
        return new SFloat(ToString(), Radix, MaxFractionLength);
    }

    public static readonly SFloat BinaryZero = new ("0", 2);
    public static readonly SFloat OctalZero = new ("0", 8);
    public static readonly SFloat DecimalZero = new ("0");
    public static readonly SFloat HexadecimalZero = new ("0", 16);
    public static SFloat Zero(int radix = 10) {
        return radix switch {
            2 => BinaryZero,
            8 => OctalZero,
            10 => DecimalZero,
            16 => HexadecimalZero,
            _ => new SFloat("0", radix)
        };
    }
    
    public static readonly SFloat BinaryOne = new ("1", 2);
    public static readonly SFloat OctalOne = new ("1", 8);
    public static readonly SFloat DecimalOne = new ("1");
    public static readonly SFloat HexadecimalOne = new ("1", 16);
    public static SFloat One(int radix = 10) {
        return radix switch {
            2 => BinaryOne,
            8 => OctalOne,
            10 => DecimalOne,
            16 => HexadecimalOne,
            _ => new SFloat("1", radix)
        };
    }

    public int FractionLength => Math.Min(Digits.Length - FloatPointIndex - 1, MaxFractionLength);

    public int IntegerLength => FloatPointIndex + 1;
    
    public bool IsInteger => FractionLength == 0;
    
    public bool IsZero => Digits == "0";

    public bool IsFractional => !IsInteger;

    public SFloat IntegerPart => Clone(
        digits: new string(GetDigitsIn(end: 0)),
        floatPointIndex: IntegerLength - 1
    );

    public SFloat FractionalPart => Clone(
        digits: "0" + new string(GetDigitsIn(start: -1)),
        floatPointIndex: 0
    );

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

    public char GetDigitAtAbs(int index) {
        if (index < 0 || index >= Digits.Length) return '0';
        return Digits[index];
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
        var rtn = DecimalZero;
        return rtn.SetDigitAt(index, GetDigitValue(GetDigitAt(index)));
    }

    /// <summary>
    /// Moves the float point to the left or right by the specified shift.
    /// </summary>
    /// <param name="shift">
    /// The shift amount. Positive value makes the absolute value of the SFloat greater (moves to the right),
    /// negative value makes the absolute value of the SFloat smaller (moves to the left).
    /// </param>
    /// <returns>The SFloat with its float point moved.</returns>
    public SFloat MoveFloatPoint(int shift) {
        var newFloatPointIndex = FloatPointIndex + shift;
        var newDigits          = Digits;

        if (newFloatPointIndex < 0) {
            newDigits = new string('0', -newFloatPointIndex) + Digits;
            newFloatPointIndex = 0;
        }
        if (newFloatPointIndex >= Digits.Length) newDigits = Digits + new string('0', newFloatPointIndex - Digits.Length + 1);

        return Clone(digits: newDigits, floatPointIndex: newFloatPointIndex);
    }

    public static bool TryParse(string s, out SFloat? result, int? radix = null) {
        radix ??= 10;
        try {
            result = new SFloat(s, radix.Value);
            return true;
        } catch {
            result = null;
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

    public bool Equals(SFloat other) {
        return string.Equals(Digits, other.Digits, StringComparison.InvariantCultureIgnoreCase)
            && FloatPointIndex == other.FloatPointIndex
            && Radix == other.Radix
            && IsNegative == other.IsNegative
            && MaxFractionLength == other.MaxFractionLength;
    }

    public override bool Equals(object? obj) {
        return obj is SFloat other && Equals(other);
    }

    public override int GetHashCode() {
        var hashCode = new HashCode();
        hashCode.Add(Digits, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(FloatPointIndex);
        hashCode.Add(Radix);
        hashCode.Add(IsNegative);
        hashCode.Add(MaxFractionLength);
        return hashCode.ToHashCode();
    }
}