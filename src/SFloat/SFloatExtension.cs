namespace SFloat;

public static class SFloatExtension {
    
    /// <summary>
    /// Converts the SFloat to a decimal (10) radix SFloat.
    /// </summary>
    /// <param name="flt">The SFloat to be converted.</param>
    /// <returns>The SFloat in decimal format.</returns>
    public static SFloat ToDecimal(this SFloat flt) {
        var intDigits  = flt.GetIntegerDigits();
        var intProduct = SFloat.Zero;
        for (var i = 0; i < intDigits.Length - 1; i++) {
            intProduct += SFloat.GetDigitValue(intDigits[i]) * flt.Radix + SFloat.GetDigitValue(intDigits[i + 1]);
        }

        var fracDigits  = flt.GetFractionalDigits();
        var fracProduct = SFloat.Zero;
        for (var i = 0; i < fracDigits.Length - 1; i++) {
            fracProduct += SFloat.GetDigitValue(fracDigits[i]) * flt.Radix + SFloat.GetDigitValue(fracDigits[i + 1]);
        }

        var str = $"{intProduct}";
        if (fracProduct != SFloat.Zero) {
            str += $".{fracProduct}";
        }
        if (flt.IsNegative) {
            str = $"-{str}";
        }
        return new SFloat(str, 10, flt.MaxFractionLength);
    }
    
    public static SFloat ToRadix(this SFloat flt, int radix) {
        return DecimalToRadix(flt.ToDecimal(), radix); // temporary TODO: implement
    }
    
    private static SFloat DecimalToRadix(SFloat flt, int radix) {
        return flt; // temporary TODO: implement
    }
}