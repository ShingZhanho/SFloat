namespace SFloat;

public static class SFloatExtension {
    
    /// <summary>
    /// Converts the SFloat to a decimal (10) radix SFloat.
    /// </summary>
    /// <param name="flt">The SFloat to be converted.</param>
    /// <returns>The SFloat in decimal format.</returns>
    public static SFloat ToDecimal(this SFloat flt) {
        return flt; // temporary TODO: implement
    }
    
    public static SFloat ToRadix(this SFloat flt, int radix) {
        return DecimalToRadix(flt.ToDecimal(), radix); // temporary TODO: implement
    }
    
    private static SFloat DecimalToRadix(SFloat flt, int radix) {
        return flt; // temporary TODO: implement
    }
}