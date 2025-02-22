namespace JacobS.SFloat.Test;

public class TypeCastTests {
    [Theory]
    [InlineData("FF", 16, 255)]
    public void SFloatToIntTest(string num, int radix, int expected) {
        // Arrange
        var flt = new SFloat(num, radix);
        
        // Act
        var result = (int)flt;
        
        // Assert
        Assert.Equal(expected, result);
    }
}