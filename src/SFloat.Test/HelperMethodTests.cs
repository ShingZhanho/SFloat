namespace SFloat.Test;

public class HelperMethodTests {
    [Theory]
    [InlineData("13", 1, "10")]
    [InlineData("13.089", -2, "0.08")]
    public void ExtractDigitAtTest(SFloat flt, int index, SFloat expected) {
        // Arrange
        ;
        
        // Act
        var result = flt.ExtractDigitAt(index);
        
        // Assert
        Assert.Equal(expected, result);
    }
}