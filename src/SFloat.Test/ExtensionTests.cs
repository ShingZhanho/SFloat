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
}