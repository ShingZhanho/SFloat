namespace JacobS.SFloat;

public static class SFloatExtension {
    
    /// <summary>
    /// Converts the SFloat to a decimal (10) radix SFloat.
    /// </summary>
    /// <param name="flt">The SFloat to be converted.</param>
    /// <returns>The SFloat in decimal format.</returns>
    public static SFloat ToDecimal(this SFloat flt) {
        if (flt.Radix == 10) return flt;
        if (flt == SFloat.DecimalZero) return new SFloat("0", 10);
        
        var intDigits  = flt.GetIntegerDigits();
        var intProduct = SFloat.DecimalZero;
        for (var i = 0; i < intDigits.Length - 1; i++) { // more than one digit
            intProduct += SFloat.GetDigitValue(intDigits[i]) * flt.Radix + SFloat.GetDigitValue(intDigits[i + 1]);
        }
        if (intDigits.Length == 1)  // only one digit
            intProduct = SFloat.GetDigitValue(intDigits[0]);

        var fracProduct = SFloat.DecimalZero;
        if (flt.IsFractional) {
            var fracDigits  = flt.GetFractionalDigits();
            for (var i = fracDigits.Length - 1; i >= 0; i--) {
                fracProduct = fracProduct / flt.Radix +
                               SFloat.GetDigitValue(fracDigits[i]);
            }
            fracProduct /= flt.Radix;
        }

        var str = $"{intProduct}";
        if (fracProduct != SFloat.DecimalZero) {
            str += $".{new string(fracProduct.GetFractionalDigits())}";
        }
        if (flt.IsNegative) {
            str = $"-{str}";
        }
        return new SFloat(str, 10, flt.MaxFractionLength);
    }

    private static readonly int[] RADIX_PWR_OF_TWO = [2, 4, 8, 16, 32];  // Supported radix powers of two.
                                                                         // Conversions to and from these radixes are
                                                                         // handled by bit manipulation.
    
    public static SFloat ToRadix(this SFloat flt, int radix) {
        if (flt.Radix == radix) return flt;
        if (flt == SFloat.DecimalZero) return new SFloat("0", radix);
        if (RADIX_PWR_OF_TWO.Contains(flt.Radix) && RADIX_PWR_OF_TWO.Contains(radix)) {
            // Convert between radixes that are powers of two.
            return PwrOfTwoConvert(flt, radix);
        }
        
        // Otherwise, convert to decimal and then to the target radix.
        return DecimalToRadix(flt.ToDecimal(), radix);
    }

    private static SFloat PwrOfTwoConvert(SFloat flt, int radix) {
        var toPower = (int)Math.Log2(radix);

        var intBits = new List<char>();
        foreach (var digit in flt.GetIntegerDigits()) 
            intBits.AddRange(SFloat.GetBitsFromValue(SFloat.GetDigitValue(digit), flt.Radix));
        while (intBits.Count % toPower != 0) intBits.Insert(0, '0');
        var intPart = "";
        for (var i = 0; i < intBits.Count / toPower; i++) 
            intPart += SFloat.GetDigitChar(SFloat.GetValueFromBits(intBits[(i * toPower)..(i * toPower + toPower)].ToArray()));
        var str = intPart;
        
        if (flt.IsFractional) {
            var fracBits = new List<char>();
            foreach (var digit in flt.GetFractionalDigits()) 
                fracBits.AddRange(SFloat.GetBitsFromValue(SFloat.GetDigitValue(digit), flt.Radix));
            while (fracBits.Count % toPower != 0) fracBits.Add('0');
            var fracPart = "";
            for (var i = 0; i < fracBits.Count / toPower; i++) 
                fracPart += SFloat.GetDigitChar(SFloat.GetValueFromBits(fracBits[(i * toPower)..(i * toPower + toPower)].ToArray()));
            str += $".{fracPart}";
        }
        
        if (flt.IsNegative) str = $"-{str}";
        return new SFloat(str, radix, flt.MaxFractionLength);
    }
    
    private static SFloat DecimalToRadix(SFloat flt, int radix) {
        var    unitRadix       = new SFloat(radix.ToString(), maxFractionLength: flt.MaxFractionLength);
        var    convertedDigits = new List<char>();
        var    quotient        = flt.IntegerPart;

        do {
            (quotient, var remainder) = SFloat.DivRem(quotient, unitRadix);
            if (remainder < 0) remainder = -remainder;
            convertedDigits.Insert(0, SFloat.GetDigitChar(remainder));
        } while (quotient != SFloat.DecimalZero);
        var str = new string(convertedDigits.ToArray());

        if (flt.IsFractional) {
            str += ".";
            convertedDigits.Clear();
            var maxIterations = flt.MaxFractionLength;
            var product = flt.FractionalPart;

            while (maxIterations-- > 0) {
                product *= unitRadix;
                if (product < 0) product = -product;
                convertedDigits.Add(SFloat.GetDigitChar(product.IntegerPart));
                product = product.FractionalPart;
                if (product == SFloat.DecimalZero) break;
            }
            
            str += new string(convertedDigits.ToArray());
        }
        
        if (flt.IsNegative) str = $"-{str}";
        
        return new SFloat(str, radix, flt.MaxFractionLength);
    }
    
    // Radix conversion shorthands
    public static SFloat ToBinary(this SFloat flt) => flt.ToRadix(2);
    public static SFloat ToOctal(this SFloat flt) => flt.ToRadix(8);
    public static SFloat ToHexadecimal(this SFloat flt) => flt.ToRadix(16);
}