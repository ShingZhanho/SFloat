namespace JacobS.SFloat.Test;

public class ExtensionTests {
    [Theory]
    [InlineData("F", 16, "15")]
    [InlineData("FF", 16, "255")]
    [InlineData("FF.33", 16, "255.19921875")]
    [InlineData("-7.3", 8, "-7.375")]
    [InlineData("12.5", 16, "18.3125")]
    public void ToDecimalTest(string num, int radix, string expected) {
        // Arrange
        var flt = new JacobS.SFloat.SFloat(num, radix);
        
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
        var flt = new JacobS.SFloat.SFloat(fromNum, fromRadix);
        
        // Act
        var result = flt.ToRadix(toRadix);
        
        // Assert
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData("11", 2, "1011")]
    [InlineData("-19.7421875", 8, "-23.574")]
    [InlineData("0.25", 2, "0.01")] // Issue #1
    [InlineData("1654", 16, "676")]
    [InlineData("156.75", 16, "9C.C")]
    public void DecimalToRadixConversionTest(JacobS.SFloat.SFloat from, int toRadix, string expected) {
        // Arrange
        ;
        
        // Act
        var result = from.ToRadix(toRadix);
        
        // Assert
        Assert.Equal(expected, result.ToString());
    }
}