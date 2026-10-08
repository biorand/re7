using Biohazard.BioRand.RE7.Services;
using IntelOrca.Biohazard.BioRand;

namespace Biohazard.BioRand.RE7.Tests;

public class ItemRandomizerWeightValidationTests {
    [Theory]
    [InlineData(200000)]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GenericItemPool_RejectsInvalidWeightsBeforeAllocating(double ratio) {
        using var randomizer = CreateRandomizer();
        var settings = new RandomItemSettings {
            ItemRatioKeyFunc = id => id switch {
                "Herb" => ratio,
                "Gunpowder" => 0.0001,
                _ => 0,
            },
        };

        var error = Assert.Throws<RandomizerUserException>(() =>
            randomizer.ItemRandomizer.CreateGeneralItemPool(settings, randomizer.GetRng("weight-validation")));

        Assert.Contains("Herb", error.Message);
        Assert.Contains("between 0 and 1", error.Message);
    }

    [Fact]
    public void GenericItemPool_PreservesValidBoundaryAndFractionalWeights() {
        using var randomizer = CreateRandomizer();
        var settings = new RandomItemSettings {
            ItemRatioKeyFunc = id => id switch {
                "Herb" => 1,
                "Gunpowder" => 0.0001,
                _ => 0,
            },
        };

        var bag = randomizer.ItemRandomizer.CreateGeneralItemPool(settings, randomizer.GetRng("weight-validation"));
        var items = bag.Next(bag.Count);

        Assert.Equal(10001, bag.Count);
        Assert.Equal(10000, items.Count(id => id == "Herb"));
        Assert.Single(items, id => id == "Gunpowder");
    }

    private static Randomizer CreateRandomizer()
        => new(new RandomizerInput { Configuration = RandomizerTest.CreateFeatureTestConfiguration() },
            "unused", new EmptyReporter());
}
