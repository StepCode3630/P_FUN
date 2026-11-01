using PlotThoseLines.MyClass;
using PlotThoseLines.ViewModels;
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

    [Fact]
    public void Filter_IsABiggerDateThan_Returns_True_When_Date_Is_Bigger()
    {
        // Arrange
        int date1 = 2024;
        int date2 = 2023;

        // Act
        bool result = date1.IsABiggerDateThan(date2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Filter_IsAValidYear_Returns_True_For_Valid_Year()
    {
        // Arrange
        int validYear = 2024;

        // Act
        bool result = validYear.IsAValidYear();

        // Assert
        Assert.True(result);
    }
}
