/*
 * SFloatOperator.cs
 *
 * This file is part of the SFloat struct. All the overloaded operators are defined in this file.
 */

using System.Globalization;

namespace SFloat;

public readonly partial struct SFloat {
    public static bool operator true(SFloat flt) {
        return flt != DecimalZero;
    }
    
    public static bool operator false(SFloat flt) {
        return flt == DecimalZero;
    }

    public static SFloat operator -(SFloat flt) {
        return flt.Clone(isNegative: !flt.IsNegative);
    }

    public static SFloat operator +(SFloat flt1, SFloat flt2) {
        // Optimization: If one of the operands is zero, return the other operand.
        if (flt1 == DecimalZero) return flt2;
        if (flt2 == DecimalZero) return flt1;
        
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
        return result.Clone(isNegative: flt.IsNegative != factor < 0);
    }
    
    public static SFloat operator *(int factor, SFloat flt) {
        return flt * factor;
    }
}