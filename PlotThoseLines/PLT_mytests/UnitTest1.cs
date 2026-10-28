using PlotThoseLines.MyClass;
using PlotThoseLines.Views;
using Xunit;

namespace PLT_mytests;

public class UnitTest1
{
    [Fact]
    public void ToString_Returns_Formatted_Message()
    {
        // Arrange
        var filter = new Filter("2024-03-01", "2024-03-10");

        // Act
        var result = filter.ToString();

        // Assert
        Assert.Equal("Start Date: 2024-03-01, End Date: 2024-03-10", result);
    }
    
}
