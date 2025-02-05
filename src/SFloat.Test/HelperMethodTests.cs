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
    
    [Theory]
    [InlineData("13", 1, "130")]
    [InlineData("-13.089", -2, "-0.13089")]
    public void MoveFloatPointTest(SFloat source, int shift, SFloat expected) {
        // Arrange
        ;
        
        // Act
        var result = source.MoveFloatPoint(shift);
        
        // Assert
        Assert.Equal(expected, result);
    }
}