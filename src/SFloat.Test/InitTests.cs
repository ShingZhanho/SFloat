namespace JacobS.SFloat.Test;

public class InitTests {
    [Theory]
    [InlineData("2", 10, "2")]
    [InlineData("0.05", 10, "0.05")]
    [InlineData("0.33333333", 10, "0.33", 2)]
    public void InitFromStringTest(string source, int radix, string expected, int? maxFracLength = null) {
        // Arrange
        maxFracLength ??= SFloat.DEFAULT_MAX_FRAC_LENGTH();
        
        // Act
        var flt = new SFloat(source, radix, maxFracLength);
        
        // Assert
        Assert.Equal(expected, flt.ToString());
    }
}