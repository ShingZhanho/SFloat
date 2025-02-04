namespace SFloat.Test;

public class ExtensionTests {
    [Theory]
    [InlineData("FF", 16, "255")]
    [InlineData("FF.33", 16, "255.51")]
    public void ToDecimalTest(string num, int radix, string expected) {
        // Arrange
        var flt = new SFloat(num, radix);
        
        // Act
        var result = flt.ToDecimal();
        
        // Assert
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData("11111111", 2, 16, "FF")]
    [InlineData("428.6A", 16, 16, "428.6A")]
    [InlineData("-127.35", 8, 16, "-57.74")]
    public void PowerOfTwoConversionTest(string fromNum, int fromRadix, int toRadix, string expected) {
        // Arrange
        var flt = new SFloat(fromNum, fromRadix);
        
        // Act
        var result = flt.ToRadix(toRadix);
        
        // Assert
        Assert.Equal(expected, result.ToString());
    }
}