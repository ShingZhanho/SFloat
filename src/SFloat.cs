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
        _radix = radix.Value;
        _digits = new List<char>();
        
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
                _floatPointIndex = _digits.Count;
            }

            var digitValue = GetDigitValue(value[i]);
            if (digitValue < 0 || digitValue > digitUpperBound)  // Check for invalid digit.
                throw new FormatException("The float contains invalid digits.");
            _digits.Add(value[i]);
        }
    }

    private List<char> _digits;        // The digits of the float.
    private int _floatPointIndex = -1; // The index of the float point.
    private readonly int _radix;       // The radix of the float.
    private bool _isNegative;          // Whether the float is negative.

    public static SFloat operator -(SFloat flt) {
        flt._isNegative = !flt._isNegative;
        return flt;
    }
    
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

    public override string ToString() {
        var chars = new char[_digits.Count + 2];
        if (_isNegative) chars[0] = '-';
        for (var i = 0; i < _digits.Count; i++) {
            if (i < _floatPointIndex) chars[_isNegative ? i + 1 : i] = _digits[i];     // Before the float point.
            else if (i == _floatPointIndex) chars[_isNegative ? i + 1 : i] = '.';     // At the float point.
            else chars[_isNegative ? i + 2 : i + 1] = _digits[i];                     // After the float point.
        }
        return new string(chars);
    }
}