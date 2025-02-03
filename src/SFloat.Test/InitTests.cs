namespace SFloat.Test;

public class InitTests {
    [Theory]
    [InlineData("2", 10, "2")]
    [InlineData("0.05", 10, "0.05")]
    public void InitFromStringTest(string source, int radix, string expected) {
        // Arrange
        var flt = new SFloat(source, radix);
        
        // Act
        ;
        
        // Assert
        Assert.Equal(expected, flt.ToString());
    }
}