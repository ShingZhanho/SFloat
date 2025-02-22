namespace JacobS.SFloat.Test;

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
    [InlineData("10.1", 2, "18", 10, "101101")]
    [InlineData("0.25", 10, "2", 10, "0.5")] // Issue #2
    public void MultiplicationTest(string numA, int radixA, string numB, int radixB, string expected) {
        // Arrange
        var a = new SFloat(numA, radixA);
        var b = new SFloat(numB, radixB);
        
        // Act
        var result = a * b;
        
        // Assert
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData("25", 10, "5", 10, "5")]
    [InlineData("8", 16, "8", 16, "1")]
    [InlineData("125", 10, "2", 10, "62.5")]
    [InlineData("1", 10, "3", 10, "0.3333333333", 10)]
    [InlineData("3.1415926535", 10, "10.111", 10, "0.3107103801305508851745623578281", 31)]
    [InlineData("58.125", 10, "0.8", 16, "116.25")]
    public void DivisionTest(string numA, int radixA, string numB, int radixB, string expected, int? maxFracLength = null) {
        // Arrange
        maxFracLength ??= SFloat.DEFAULT_MAX_FRAC_LENGTH();
        var a = new SFloat(numA, radixA, maxFracLength);
        var b = new SFloat(numB, radixB, maxFracLength);
        
        // Act
        var result = a / b;
        
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