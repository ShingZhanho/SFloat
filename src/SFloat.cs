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
    }
    
    private static int GetDigitValue(char digit) {
        return digit switch {
            // Get the value of the digit. Maximum supported radix: 36.
            // Returns -1 if the digit is invalid.
            >= '0' and <= '9' => digit - '0',
            >= 'a' and <= 'z' => digit - 'a' + 10,
            >= 'A' and <= 'Z' => digit - 'A' + 10,
            _                 => -1
        };
    }
}