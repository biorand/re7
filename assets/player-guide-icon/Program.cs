using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using IntelOrca.Biohazard.REE.Graphics;

// dotnet run --project assets/player-guide-icon -- <original ui0101_01_iam.tex.35> <output.tex.35>
// Replace only Mia's email tile (80,80)-(160,160); do not recompress neighboring icons.
if (args.Length != 2) throw new ArgumentException("Expected original atlas path and output path.");
var atlas = File.ReadAllBytes(args[0]);
var texture = new TextureFile(atlas);
if (texture.RawVersion != 35 || texture.Width != 512 || texture.Height != 256 ||
    texture.FormatId != 99 || texture.MipCount != 1 || texture.ImageCount != 1 || atlas.Length != 131128)
    throw new InvalidDataException("Expected the RE7 RT 512x256 BC7 inventory atlas.");

// Authored warning triangle, with a dark border and exclamation mark. Supersampling
// keeps the outline readable at the native 80px size and smaller inventory scales.
var pixels = new byte[80 * 80 * 4];
for (var y = 0; y < 80; y++) {
    for (var x = 0; x < 80; x++) {
        var red = 0; var green = 0; var blue = 0; var covered = 0;
        for (var sy = 0; sy < 4; sy++) {
            for (var sx = 0; sx < 4; sx++) {
                var px = x + (sx + 0.5) / 4;
                var py = y + (sy + 0.5) / 4;
                if (py < 6 || py > 73 || Math.Abs(px - 40) > (py - 6) * 0.53) continue;
                var interior = py >= 15 && py <= 68 && Math.Abs(px - 40) <= (py - 15) * 0.53;
                var mark = (px >= 36 && px <= 44 && py >= 30 && py <= 51) ||
                    Math.Pow(px - 40, 2) + Math.Pow(py - 60, 2) <= 20;
                var yellow = interior && !mark;
                red += yellow ? 255 : 25; green += yellow ? 199 : 22; blue += yellow ? 40 : 15;
                covered++;
            }
        }
        if (covered == 0) continue;
        var offset = (y * 80 + x) * 4;
        pixels[offset] = (byte)(red / covered);
        pixels[offset + 1] = (byte)(green / covered);
        pixels[offset + 2] = (byte)(blue / covered);
        pixels[offset + 3] = (byte)(covered * 255 / 16);
    }
}
var encoder = new BcEncoder();
encoder.OutputOptions.Format = CompressionFormat.Bc7;
encoder.OutputOptions.GenerateMipMaps = false;
encoder.OutputOptions.Quality = CompressionQuality.BestQuality;
var blocks = encoder.EncodeToRawBytes(pixels, 80, 80, PixelFormat.Rgba32)[0];
// TEX v35 has a 40-byte header and one 16-byte mip descriptor here. Each BC7 block
// covers 4x4 pixels in 16 bytes; the atlas row stride is 128 blocks (2048 bytes).
for (var row = 0; row < 20; row++)
    blocks.AsSpan(row * 320, 320).CopyTo(atlas.AsSpan(56 + (20 + row) * 2048 + 320, 320));
File.WriteAllBytes(args[1], atlas);
Console.WriteLine($"Wrote {args[1]} (only the 80x80 email tile changed).");
