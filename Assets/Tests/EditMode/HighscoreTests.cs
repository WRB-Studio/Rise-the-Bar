using NUnit.Framework;

public class HighscoreTests
{
    [TestCase(10f, 200, 200)]
    [TestCase(12.5f, 300, 375)]
    [TestCase(0f, 200, 0)]
    [TestCase(-1f, 200, 0)]
    [TestCase(10f, -1, 0)]
    [TestCase(10f, 0, 0)]
    public void HighscorePreservesFormulaAndRejectsNegativeValues(float distance, int accuracy, int expected)
    {
        Assert.That(SaveLoadManager.calculateHighscore(distance, accuracy), Is.EqualTo(expected));
    }

    [Test]
    public void NonFiniteDistanceDoesNotProduceInvalidHighscore()
    {
        Assert.That(SaveLoadManager.calculateHighscore(float.NaN, 200), Is.Zero);
        Assert.That(SaveLoadManager.calculateHighscore(float.PositiveInfinity, 200), Is.Zero);
    }

    [Test]
    public void VeryLargeHighscoreSaturatesInsteadOfOverflowing()
    {
        Assert.That(SaveLoadManager.calculateHighscore(float.MaxValue, int.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.That(SaveLoadManager.calculateHighscore(21474836f, 1000), Is.EqualTo(2147483600));
    }
}
