using IntelOrca.Biohazard.BioRand;

namespace Biohazard.BioRand.RE7.Tests;

public class RngTests {
    [Fact]
    public void NextInclusive_CanReturnUpperBound() {
        var rng = new Rng(0);
        var values = Enumerable.Range(0, 100)
            .Select(_ => rng.NextInclusive(1, 2))
            .ToHashSet();

        Assert.Contains(1, values);
        Assert.Contains(2, values);
    }

    [Fact]
    public void NextGuid_CreatesVersion4Rfc4122Guid() {
        var guid = new Rng(0).NextGuid();
        var bytes = guid.ToByteArray();

        Assert.Equal(4, (bytes[7] & 0xF0) >> 4);
        Assert.Equal(0x80, bytes[8] & 0xC0);
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void GetRng_DoesNotCollideForAmbiguousKeyConcatenations() {
        using var left = CreateRandomizer(1);
        using var right = CreateRandomizer(12);

        Assert.NotEqual(
            left.GetRng(23).Next(),
            right.GetRng(3).Next());
        Assert.NotEqual(
            left.GetRng("ab", "c").Next(),
            left.GetRng("a", "bc").Next());
    }

    private static Randomizer CreateRandomizer(int seed) {
        var input = new RandomizerInput(){
            Seed = seed,
            Configuration = RandomizerTest.CreateFeatureTestConfiguration()
        };
        return new Randomizer(input, RandomizerTest.InputPakPath, new EmptyReporter());
    }
}
