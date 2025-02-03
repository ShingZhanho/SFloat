namespace SFloat.Test;

public class ArithmeticOperationsTests {
    [Theory]
    [InlineData("2", 10, "5", 10, "7")]
    [InlineData("8", 16, "8", 16, "10")]
    [InlineData("12.93", 10, "5.7", 10, "18.63")]
    public void AdditionTest(string numA, int baseA, string numB, int baseB, string expected) {
        // Arrange
        var a = new SFloat(numA, baseA);
        var b = new SFloat(numB, baseB);
        
        // Act
        var result = a + b;
        
        // Assert
        Assert.Equal(numA, a.ToString());
        Assert.Equal(numB, b.ToString());
        Assert.Equal(expected, result.ToString());
    }
}