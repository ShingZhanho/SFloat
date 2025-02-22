/*
 * SFloatOperator.cs
 *
 * This file is part of the SFloat struct. All the implicit and explicit type conversions are defined in this file.
 */

namespace JacobS.SFloat;

public readonly partial struct SFloat {
    public static implicit operator SFloat(string s) => new (s);
    
    public static implicit operator string(SFloat flt) => flt.ToString();
    
    public static implicit operator SFloat(int i) => new (i.ToString());

    public static implicit operator int(SFloat flt) {
        // For a number (a_n-1 a_n-2 ... a_1 a_0)_r, the value in decimal is:
        //      r * (r * (r * a_n-1 + a_n-2) + a_n-3) + ... + a_1) + a_0
        
        // Any fractional part will be dropped directly.
        try {
            var result = GetDigitValue(flt.GetDigitAt(flt.IntegerLength - 1)); // a_n-1
            for (var i = flt.IntegerLength - 1; i > 0; i--) {
                checked {
                    result = result * flt.Radix + GetDigitValue(flt.GetDigitAt(i - 1));
                }
            }

            return flt.IsNegative ? -result : result;
        } catch {
            throw new OverflowException("The SFloat represents a value that is out of the range of Int32.");
        }
    }
}