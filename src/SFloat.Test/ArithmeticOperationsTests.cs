namespace SFloat.Test;

public class ArithmeticOperationsTests {
    [Theory]
    [InlineData("2", 10, "5", 10, "7")]
    [InlineData("8", 16, "8", 16, "10")]
    [InlineData("12.93", 10, "5.7", 10, "18.63")]
    [InlineData("10010.01001", 2, "1011.101", 2, "11101.11101")]
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
    
    [Theory]
    [InlineData("2", 10, "5", 10, "-3")]
    public void SubtractionTest(string numA, int baseA, string numB, int baseB, string expected) {
        // Arrange
        var a = new SFloat(numA, baseA);
        var b = new SFloat(numB, baseB);
        
        // Act
        var result = a - b;
        
        // Assert
        Assert.Equal(numA, a.ToString());
        Assert.Equal(numB, b.ToString());
        Assert.Equal(expected, result.ToString());
    }
    
    [Theory]
    [InlineData("25", 10, "41", 10, "1025")]
    [InlineData("-8", 16, "8", 16, "-40")]
    [InlineData("23.4", 10, "36.7", 10, "858.78")]
    [InlineData("3A.2", 16, "12.5", 16, "428.6A")]
    [InlineData("0.00", 10, "19.27", 10, "0")]
    // [InlineData("10.101", 2, "18", 10, "101101")] TODO: Implement conversion to any radix
    public void Multiplication_SFloatSFloat_Test(string numA, int radixA, string numB, int radixB, string expected) {
        // Arrange
        var a = new SFloat(numA, radixA);
        var b = new SFloat(numB, radixB);
        
        // Act
        var result = a * b;
        
        // Assert
        Assert.Equal(expected, result.ToString());
    }
    
    [Theory]
    [InlineData("20.500", 10, "0020.5", 10)]
    public void EqualityTest(string numA, int baseA, string numB, int baseB) {
        // Arrange
        var a = new SFloat(numA, baseA);
        var b = new SFloat(numB, baseB);
        
        // Act
        ;
        
        // Assert
        Assert.True(a == b);
    }
}