/*
 * SFloatOperator.cs
 *
 * This file is part of the SFloat struct. All the implicit and explicit type conversions are defined in this file.
 */

namespace SFloat;

public readonly partial struct SFloat {
    public static implicit operator SFloat(string s) => new (s);
    
    public static implicit operator string(SFloat flt) => flt.ToString();
    
    public static implicit operator SFloat(int i) => new (i.ToString());

    public static implicit operator int(SFloat flt) {
        // Any fractional part will be dropped directly.
        try {
            var result = 0;
            for (var i = flt.IntegerLength; i >= 0; i--) {
                result = checked(result + GetDigitValue(flt.GetDigitAt(i)) * flt.Radix +
                                 GetDigitValue(flt.GetDigitAt(i - 1)));
            }

            return flt.IsNegative ? -result : result;
        } catch {
            throw new OverflowException("The SFloat represents a value that is out of the range of Int32.");
        }
    }
}