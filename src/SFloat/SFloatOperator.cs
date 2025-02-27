/*
 * SFloatOperator.cs
 *
 * This file is part of the SFloat struct. All the overloaded operators are defined in this file.
 */

using System.Globalization;

namespace JacobS.SFloat;

public readonly partial struct SFloat {
    public static bool operator true(SFloat flt) {
        return !flt.IsZero;
    }
    
    public static bool operator false(SFloat flt) {
        return flt.IsZero;
    }

    public static SFloat operator -(SFloat flt) {
        return flt.Clone(isNegative: !flt.IsNegative).EnsureZeroTruncation();
    }

    public static SFloat operator +(SFloat flt1, SFloat flt2) {
        // Optimization: If one of the operands is zero, return the other operand.
        if (flt1.IsZero) return flt2;
        if (flt2.IsZero) return flt1;
        
        // Handle negative numbers. Two operands must be positive for addition.
        if (flt1.IsNegative && !flt2.IsNegative) return flt2 - -flt1;
        if (!flt1.IsNegative && flt2.IsNegative) return flt1 - -flt2;
        if (flt1.IsNegative && flt2.IsNegative) return -(-flt1 + -flt2);
        
        // Handle mismatched radix. Always convert to the radix of the first operand.
        if (flt1.Radix != flt2.Radix) flt2 = flt2.ToRadix(flt1.Radix);
        
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

    public static SFloat operator ++(SFloat flt) {
        return flt + One(flt.Radix);
    }
    
    public static SFloat operator -(SFloat flt1, SFloat flt2) {
        // Optimisation: if the first operand is zero, return the negation of the second operand.
        if (flt1.IsZero) return -flt2;
        // if the second operand is zero, return the first operand.
        if (flt2.IsZero) return flt1;
        
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
        if (flt1.Radix != flt2.Radix) flt2 = flt2.ToRadix(flt1.Radix);
        
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
        
        result.Insert(maxIntLength, '.');

        return new SFloat(new string(result.ToArray()), flt1.Radix,
            Math.Max(flt1.MaxFractionLength, flt2.MaxFractionLength));
    }
    
    public static SFloat operator --(SFloat flt) {
        return flt - One(flt.Radix);
    }
    
    public static bool operator >(SFloat flt1, SFloat flt2) {
        // Handle zero comparison
        if (flt2.IsZero) return !flt1.IsNegative;
        
        // Handle different signs.
        if (flt1.IsNegative && !flt2.IsNegative) return false;
        if (!flt1.IsNegative && flt2.IsNegative) return true;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1.Radix != flt2.Radix) {
            flt1 = flt1.ToDecimal();
            flt2 = flt2.ToDecimal();
        }
        
        if (flt1 == flt2) return false;
        
        // Compare digit by digit.
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        for (var i = maxIntLength - 1; i >= 0; i--) { // Integer part
            if (GetDigitValue(flt1.GetDigitAt(i)) < GetDigitValue(flt2.GetDigitAt(i))) return false;
            if (GetDigitValue(flt1.GetDigitAt(i)) > GetDigitValue(flt2.GetDigitAt(i))) return true;
        }

        for (var i = 1; i <= maxFracLength; i++) { // Fractional part
            if (GetDigitValue(flt1.GetDigitAt(-i)) < GetDigitValue(flt2.GetDigitAt(-i))) return false;
            if (GetDigitValue(flt1.GetDigitAt(-i)) > GetDigitValue(flt2.GetDigitAt(-i))) return true;
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
        if (flt2.IsZero) return flt1.IsZero;
        
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
        // Handle zero comparison
        if (flt2.IsZero) return flt1.IsNegative;
        
        // Handle different signs.
        if (flt1.IsNegative && !flt2.IsNegative) return true;
        if (!flt1.IsNegative && flt2.IsNegative) return false;
        
        // If the operands have different radix, convert both to decimal.
        if (flt1.Radix != flt2.Radix) {
            flt1 = flt1.ToDecimal();
            flt2 = flt2.ToDecimal();
        }
        
        if (flt1 == flt2) return false;
        
        // Compare digit by digit.
        var maxFracLength = Math.Max(flt1.FractionLength, flt2.FractionLength);
        var maxIntLength  = Math.Max(flt1.IntegerLength, flt2.IntegerLength);
        for (var i = maxIntLength - 1; i >= 0; i--) { // Integer part
            if (GetDigitValue(flt1.GetDigitAt(i)) > GetDigitValue(flt2.GetDigitAt(i))) return false;
            if (GetDigitValue(flt1.GetDigitAt(i)) < GetDigitValue(flt2.GetDigitAt(i))) return true;
        }

        for (var i = 1; i <= maxFracLength; i++) { // Fractional part
            if (GetDigitValue(flt1.GetDigitAt(-i)) > GetDigitValue(flt2.GetDigitAt(-i))) return false;
            if (GetDigitValue(flt1.GetDigitAt(-i)) < GetDigitValue(flt2.GetDigitAt(-i))) return true;
        }
        
        return true;
    }
    
    public static bool operator <=(SFloat flt1, SFloat flt2) {
        return flt1 < flt2 || flt1 == flt2;
    }

    public static SFloat operator *(SFloat flt, int factor) {
        if (factor == 0) return Zero(flt.Radix);
        if (factor == 1) return flt;
        if (factor < 0) return -(flt * -factor);
        var result = flt;
        for (var i = 0; i < factor; i++) {
            result += flt;
        }
        return result.Clone(isNegative: flt.IsNegative != factor < 0).EnsureZeroTruncation();
    }
    
    public static SFloat operator *(int factor, SFloat flt) {
        return flt * factor;
    }

    public static SFloat operator *(SFloat flt1, SFloat flt2) {
        // Handle zero multiplication
        if (flt1.IsZero || flt2.IsZero) return Zero(flt1.Radix);

        if (flt1.Radix != flt2.Radix) flt2 = flt2.ToRadix(flt1.Radix);
        
        // Always put the operand with fewer digits on the right side.
        if (flt1.Digits.Length < flt2.Digits.Length) return flt2 * flt1;
        
        var product = Zero(flt1.Radix);
        for (var i = flt2.Digits.Length - 1; i >= 0; i--) {
            // For each digit of flt2, right to left:
            //     Multiply flt1 by the digit;
            //     Add 0's to the right to shift the product to the left
            var stepDigit = new SFloat(flt2.GetDigitAtAbs(i).ToString(), flt2.Radix, flt2.MaxFractionLength);
            if (stepDigit.IsZero) continue; // Skip if the digit is zero
            var stepProduct = NDigitsMultiplyOneDigit(new SFloat(flt1.Digits, flt1.Radix, flt1.MaxFractionLength), stepDigit);
            stepProduct = new SFloat(
                stepProduct.Digits + new string('0', flt2.Digits.Length - i - 1),
                flt1.Radix,
                stepProduct.MaxFractionLength);
            product += stepProduct;
        }
        
        return product.MoveFloatPoint(-(flt1.FractionLength + flt2.FractionLength))
                      .Clone(isNegative: flt1.IsNegative != flt2.IsNegative)
                      .EnsureZeroTruncation();
    }

    private static SFloat NDigitsMultiplyOneDigit(SFloat nDigits, SFloat oneDigit) {
        if (oneDigit.Digits.Length != 1) throw new ArgumentException("oneDigit must be a single digit.");
        if (oneDigit.Radix != nDigits.Radix) throw new ArgumentException("Both operands must have the same radix.");
        if (oneDigit.IsZero) return Zero(nDigits.Radix);
        
        var digits  = new List<char>();
        var carry   = 0;
        for (var i = nDigits.Digits.Length - 1; i >= 0; i--) {
            var result = GetDigitValue(nDigits.GetDigitAtAbs(i)) * GetDigitValue(oneDigit.GetDigitAtAbs(0)) + carry;
            carry = 0;
            while (result >= nDigits.Radix) {
                carry++;
                result -= nDigits.Radix;
            }
            
            digits.Insert(0, GetDigitChar(result));
        }
        if (carry != 0) digits.Insert(0, GetDigitChar(carry));

        return new SFloat {
            Digits            = new string(digits.ToArray()),
            Radix             = nDigits.Radix,
            IsNegative        = false,
            FloatPointIndex   = digits.Count,
            MaxFractionLength = nDigits.MaxFractionLength
        };
    }

    public static SFloat operator /(SFloat dividend, SFloat divisor) {
        if (divisor.IsZero) throw new DivideByZeroException("Divisor cannot be zero.");
        if (dividend.IsZero) return dividend;
        
        if (dividend.Radix != divisor.Radix) divisor = divisor.ToRadix(dividend.Radix);

        // guarantee that both the dividend and the divisor are integers
        var shiftFactor = Math.Max(dividend.FractionLength, divisor.FractionLength);
        dividend = dividend.MoveFloatPoint(shiftFactor);
        divisor  = divisor.MoveFloatPoint(shiftFactor);

        var quotientDigits     = new List<char>();
        var dividendDigits     = new List<char>(dividend.Digits);
        var stepDividendDigits = new List<char>();
        
        // Perform long division

        var sigEnd = false;
        while (true) {
            SFloat stepDividend;
            do {
                stepDividendDigits.Add(dividendDigits.Pop(0));
                quotientDigits.Add('0');
                if (dividendDigits.Count == 0) dividendDigits.Add('0');
                // check if all digits in stepDividend are zero
                if (stepDividendDigits.All(d => d == '0')) {
                    quotientDigits.Pop(); // undo the last addition
                    sigEnd = true;
                    break;
                }
                stepDividend = new SFloat(
                    new string(stepDividendDigits.ToArray()),
                    dividend.Radix,
                    dividend.MaxFractionLength
                );
            } while (stepDividend < divisor); // continue until stepDividend >= divisor
            stepDividend = new SFloat( // repeated assignment to solve compiler "use before initialization" error;
                                       // optimization is needed
                new string(stepDividendDigits.ToArray()),
                dividend.Radix,
                dividend.MaxFractionLength
            );
            
            if (sigEnd) break;
            
            // find the quotient (guaranteed to be between 1 and radix)
            var stepQuotient = 0;
            var stepProduct  = Zero(dividend.Radix);
            while (true) {
                stepQuotient++;
                stepProduct += divisor;
                if (stepProduct <= stepDividend) continue;
                stepQuotient--;
                stepProduct -= divisor;
                break;
            }
            // set the last digit of the quotient to be the digit of the stepQuotient
            quotientDigits[^1] = GetDigitChar(stepQuotient);
            // check if the quotient has reached the maximum fraction length
            if (quotientDigits.Count - dividend.IntegerLength >= dividend.MaxFractionLength) break;
            // set stepDividend as the remainder
            stepDividendDigits = new List<char>((stepDividend - stepProduct).GetDigitsIn(null, null));
        }

        // insert float point
        quotientDigits.Insert(dividend.IntegerLength, '.');
        
        // insert negative sign if necessary
        if (dividend.IsNegative != divisor.IsNegative) quotientDigits.Insert(0, '-');
        
        return new SFloat(
            new string(quotientDigits.ToArray()),
            dividend.Radix,
            dividend.MaxFractionLength
        );
    }

    /// <summary>
    /// Returns the signed remainder of the division of two SFloat numbers.
    /// Modulo operation is limited to integers only. An InvalidOperationException is thrown if either the dividend or
    /// the divisor is fractional.
    /// </summary>
    public static SFloat operator %(SFloat dividend, SFloat divisor) {
        if (dividend.IsFractional || divisor.IsFractional) 
            throw new InvalidOperationException("Modulo operation is limited to integers only.");
        return DivRem(dividend, divisor).Remainder;
    }

    /// <summary>
    /// Returns the quotient and remainder of the division of two SFloat numbers.
    /// Limited to integers only. An InvalidOperationException is thrown if either the dividend or the divisor is
    /// fractional.
    /// </summary>
    /// <returns>
    /// A tuple containing the quotient and remainder of the division.
    /// </returns>
    public static (SFloat Quotient, SFloat Remainder) DivRem(SFloat dividend, SFloat divisor) {
        if (dividend.IsFractional || divisor.IsFractional) 
            throw new InvalidOperationException("Division is limited to integers only.");
        var quotient  = (dividend / divisor).IntegerPart;
        var remainder = dividend - quotient * divisor;
        return (quotient, remainder);
    }
}