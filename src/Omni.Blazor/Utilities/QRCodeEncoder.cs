using System.Text;
using Omni.Blazor.Models;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Self-contained QR Code encoder (Byte mode, UTF-8). Versions 1..40, all
/// four error-correction levels (L/M/Q/H), automatic version selection and
/// optimal mask selection per ISO/IEC 18004:2015. Returns the final NxN
/// module matrix as a 2D bool (true = dark, false = light).
/// Adapted from Nayuki's public-domain QR-Code-generator reference.
/// </summary>
internal static class QRCodeEncoder
{
    /// <summary>Encode <paramref name="text"/> at the given ECC level. Returns
    /// the module matrix (size = version*4+17 on each side).</summary>
    public static bool[,] Encode(string text, QRCodeEcc ecc)
    {
        var data = Encoding.UTF8.GetBytes(text ?? "");
        int eccIdx = MapEcc(ecc);
        int version = SelectVersion(data, eccIdx);

        // Build the bit stream: mode (4) + length (8 or 16) + bytes + terminator + padding.
        var bb = new BitBuffer();
        bb.AppendBits(0b0100, 4);                         // Byte mode
        bb.AppendBits(data.Length, NumCharCountBits(version));
        foreach (var b in data) bb.AppendBits(b, 8);

        int dataCapBits = NumDataCodewords(version, eccIdx) * 8;
        bb.AppendBits(0, Math.Min(4, dataCapBits - bb.Length));
        bb.AppendBits(0, (8 - bb.Length % 8) % 8);
        for (byte pad = 0xEC; bb.Length < dataCapBits; pad = (byte)(pad ^ (0xEC ^ 0x11)))
            bb.AppendBits(pad, 8);

        var codewords = AddEccAndInterleave(bb.ToBytes(), version, eccIdx);

        int size = version * 4 + 17;
        var modules  = new bool[size, size];
        var reserved = new bool[size, size];

        DrawFunctionPatterns(modules, reserved, version);
        DrawCodewords(modules, reserved, codewords);

        // Pick the best mask (lowest penalty score).
        int bestMask = 0; long bestScore = long.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            ApplyMask(modules, reserved, mask);
            DrawFormatBits(modules, reserved, eccIdx, mask);
            long score = ComputePenalty(modules);
            if (score < bestScore) { bestScore = score; bestMask = mask; }
            ApplyMask(modules, reserved, mask);  // XOR is self-inverse
        }
        ApplyMask(modules, reserved, bestMask);
        DrawFormatBits(modules, reserved, eccIdx, bestMask);
        return modules;
    }

    // ============================================================
    // Tables (ISO/IEC 18004 Annex)
    // ============================================================

    private const int MinVersion = 1, MaxVersion = 40;

    // [eccIdx, version] → ECC codewords per block.
    private static readonly sbyte[,] EccCodewordsPerBlock = new sbyte[,] {
        { -1,  7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }, // L
        { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 }, // M
        { -1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }, // Q
        { -1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }, // H
    };

    // [eccIdx, version] → number of error-correction blocks.
    private static readonly sbyte[,] NumEccBlocks = new sbyte[,] {
        { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2,  4,  4,  4,  4,  4,  6,  6,  6,  6,  7,  8,  8,  9,  9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 }, // L
        { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5,  5,  5,  8,  9,  9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 }, // M
        { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8,  8,  8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 }, // Q
        { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8,  8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 }, // H
    };

    // ============================================================
    // Version / capacity calculations
    // ============================================================

    private static int NumRawDataModules(int v)
    {
        int size = v * 4 + 17;
        int result = size * size;
        result -= 8 * 8 * 3;             // 3 finder patterns (8x8 incl. separator)
        result -= 15 * 2 + 1;            // format info + dark module
        result -= (size - 16) * 2;       // timing patterns (excl. finder overlap)
        if (v >= 2)
        {
            int numAlign = v / 7 + 2;
            result -= (numAlign - 1) * (numAlign - 1) * 25;
            result -= (numAlign - 2) * 2 * 20;
            if (v >= 7) result -= 18 * 2;
        }
        return result;
    }

    private static int NumDataCodewords(int v, int eccIdx)
        => NumRawDataModules(v) / 8
         - EccCodewordsPerBlock[eccIdx, v] * NumEccBlocks[eccIdx, v];

    private static int NumCharCountBits(int v) => v < 10 ? 8 : 16; // Byte mode

    private static int MapEcc(QRCodeEcc ecc) => ecc switch
    {
        QRCodeEcc.Low      => 0,
        QRCodeEcc.Medium   => 1,
        QRCodeEcc.Quartile => 2,
        QRCodeEcc.High     => 3,
        _ => 1,
    };

    private static int SelectVersion(byte[] data, int eccIdx)
    {
        for (int v = MinVersion; v <= MaxVersion; v++)
        {
            int cap = NumDataCodewords(v, eccIdx) * 8;
            int needed = 4 + NumCharCountBits(v) + data.Length * 8;
            if (needed <= cap) return v;
        }
        throw new ArgumentException(
            $"QR: {data.Length} bytes ultrapassam a capacidade da maior versão (40) com este ECC.");
    }

    // ============================================================
    // Reed–Solomon ECC
    // ============================================================

    private static byte[] AddEccAndInterleave(byte[] data, int v, int eccIdx)
    {
        int numBlocks      = NumEccBlocks[eccIdx, v];
        int blockEccLen    = EccCodewordsPerBlock[eccIdx, v];
        int rawCodewords   = NumRawDataModules(v) / 8;
        int numShortBlocks = numBlocks - rawCodewords % numBlocks;
        int shortBlockLen  = rawCodewords / numBlocks;

        var blocks = new byte[numBlocks][];
        var divisor = ReedSolomonDivisor(blockEccLen);
        int k = 0;
        for (int i = 0; i < numBlocks; i++)
        {
            int datLen = shortBlockLen - blockEccLen + (i < numShortBlocks ? 0 : 1);
            var dat = new byte[datLen];
            Array.Copy(data, k, dat, 0, datLen);
            k += datLen;
            var ecc = ReedSolomonRemainder(dat, divisor);
            var block = new byte[shortBlockLen + 1];
            Array.Copy(dat, block, datLen);
            Array.Copy(ecc, 0, block, block.Length - blockEccLen, blockEccLen);
            blocks[i] = block;
        }

        var result = new byte[rawCodewords];
        int idx = 0;
        for (int col = 0; col < blocks[0].Length; col++)
        {
            for (int row = 0; row < blocks.Length; row++)
            {
                if (col != shortBlockLen - blockEccLen || row >= numShortBlocks)
                    result[idx++] = blocks[row][col];
            }
        }
        return result;
    }

    private static byte[] ReedSolomonDivisor(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;
        byte root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < result.Length; j++)
            {
                result[j] = (byte)GfMul(result[j], root);
                if (j + 1 < result.Length) result[j] ^= result[j + 1];
            }
            root = (byte)GfMul(root, 0x02);
        }
        return result;
    }

    private static byte[] ReedSolomonRemainder(byte[] data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (var b in data)
        {
            byte factor = (byte)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = 0;
            for (int i = 0; i < result.Length; i++)
                result[i] ^= (byte)GfMul(divisor[i], factor);
        }
        return result;
    }

    private static int GfMul(int x, int y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return z & 0xFF;
    }

    // ============================================================
    // Module placement (function patterns, format/version, data)
    // ============================================================

    private static void DrawFunctionPatterns(bool[,] m, bool[,] reserved, int v)
    {
        int size = m.GetLength(0);

        // Timing patterns (rows/cols at index 6)
        for (int i = 0; i < size; i++)
        {
            SetFunc(m, reserved, 6, i, i % 2 == 0);
            SetFunc(m, reserved, i, 6, i % 2 == 0);
        }

        // Three finder patterns
        DrawFinder(m, reserved, 3, 3);
        DrawFinder(m, reserved, size - 4, 3);
        DrawFinder(m, reserved, 3, size - 4);

        // Alignment patterns (versions ≥ 2)
        var pos = GetAlignmentPositions(v);
        int n = pos.Length;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (!((i == 0 && j == 0) || (i == 0 && j == n - 1) || (i == n - 1 && j == 0)))
                    DrawAlignment(m, reserved, pos[i], pos[j]);

        // Reserve format-info area (15 modules per copy, with dark module).
        ReserveFormatArea(m, reserved);
        if (v >= 7) DrawVersion(m, reserved, v);
    }

    private static void DrawFinder(bool[,] m, bool[,] reserved, int x, int y)
    {
        for (int dy = -4; dy <= 4; dy++)
            for (int dx = -4; dx <= 4; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx < 0 || yy < 0 || xx >= m.GetLength(0) || yy >= m.GetLength(0)) continue;
                int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                SetFunc(m, reserved, xx, yy, d != 2 && d != 4);
            }
    }

    private static void DrawAlignment(bool[,] m, bool[,] reserved, int x, int y)
    {
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                SetFunc(m, reserved, x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private static void ReserveFormatArea(bool[,] m, bool[,] reserved)
    {
        int size = m.GetLength(0);
        // First copy
        for (int i = 0; i <= 5; i++) reserved[8, i] = true;
        reserved[8, 7] = true; reserved[8, 8] = true; reserved[7, 8] = true;
        for (int i = 9; i < 15; i++) reserved[14 - i, 8] = true;

        // Second copy
        for (int i = 0; i < 8; i++) reserved[size - 1 - i, 8] = true;
        for (int i = 8; i < 15; i++) reserved[8, size - 15 + i] = true;
        // Dark module
        reserved[8, size - 8] = true;
        m[8, size - 8] = true;
    }

    private static int[] GetAlignmentPositions(int v)
    {
        if (v == 1) return Array.Empty<int>();
        int numAlign = v / 7 + 2;
        int step = (v == 32) ? 26 : (v * 4 + numAlign * 2 + 1) / (numAlign * 2 - 2) * 2;
        var result = new int[numAlign];
        result[0] = 6;
        for (int i = result.Length - 1, p = v * 4 + 10; i >= 1; i--, p -= step)
            result[i] = p;
        return result;
    }

    private static void DrawVersion(bool[,] m, bool[,] reserved, int v)
    {
        int rem = v;
        for (int i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        int bits = (v << 12) | rem;
        int size = m.GetLength(0);
        for (int i = 0; i < 18; i++)
        {
            bool bit = ((bits >> i) & 1) != 0;
            int a = size - 11 + i % 3, b = i / 3;
            SetFunc(m, reserved, a, b, bit);
            SetFunc(m, reserved, b, a, bit);
        }
    }

    private static void DrawFormatBits(bool[,] m, bool[,] reserved, int eccIdx, int mask)
    {
        // ISO mapping: L=01, M=00, Q=11, H=10 (our internal L=0,M=1,Q=2,H=3)
        int isoEcc = eccIdx switch { 0 => 0b01, 1 => 0b00, 2 => 0b11, 3 => 0b10, _ => 0 };
        int data = (isoEcc << 3) | mask;
        int rem = data;
        for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        int bits = ((data << 10) | rem) ^ 0x5412;  // 15 bits

        int size = m.GetLength(0);
        // First copy
        for (int i = 0; i <= 5; i++) m[8, i] = GetBit(bits, i);
        m[8, 7] = GetBit(bits, 6);
        m[8, 8] = GetBit(bits, 7);
        m[7, 8] = GetBit(bits, 8);
        for (int i = 9; i < 15; i++) m[14 - i, 8] = GetBit(bits, i);
        // Second copy
        for (int i = 0; i < 8; i++) m[size - 1 - i, 8] = GetBit(bits, i);
        for (int i = 8; i < 15; i++) m[8, size - 15 + i] = GetBit(bits, i);
        m[8, size - 8] = true;
    }

    private static bool GetBit(int x, int i) => ((x >> i) & 1) != 0;

    private static void SetFunc(bool[,] m, bool[,] reserved, int x, int y, bool dark)
    {
        if (x < 0 || y < 0 || x >= m.GetLength(0) || y >= m.GetLength(0)) return;
        m[x, y] = dark;
        reserved[x, y] = true;
    }

    private static void DrawCodewords(bool[,] m, bool[,] reserved, byte[] data)
    {
        int size = m.GetLength(0);
        int i = 0;
        // Zigzag from bottom-right upward, skipping the vertical timing column at x=6.
        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (int vert = 0; vert < size; vert++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? size - 1 - vert : vert;
                    if (!reserved[x, y] && i < data.Length * 8)
                    {
                        m[x, y] = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                        i++;
                    }
                }
            }
        }
    }

    // ============================================================
    // Mask & penalty
    // ============================================================

    private static void ApplyMask(bool[,] m, bool[,] reserved, int mask)
    {
        int size = m.GetLength(0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (reserved[x, y]) continue;
                bool invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    7 => ((x + y) % 2 + x * y % 3) % 2 == 0,
                    _ => false,
                };
                if (invert) m[x, y] ^= true;
            }
        }
    }

    private static long ComputePenalty(bool[,] m)
    {
        int size = m.GetLength(0);
        long penalty = 0;

        // Rule 1: 5+ in a row/col of same color
        for (int y = 0; y < size; y++)
        {
            bool color = m[0, y]; int run = 1;
            for (int x = 1; x < size; x++)
            {
                if (m[x, y] == color) { run++; if (run == 5) penalty += 3; else if (run > 5) penalty++; }
                else { color = m[x, y]; run = 1; }
            }
        }
        for (int x = 0; x < size; x++)
        {
            bool color = m[x, 0]; int run = 1;
            for (int y = 1; y < size; y++)
            {
                if (m[x, y] == color) { run++; if (run == 5) penalty += 3; else if (run > 5) penalty++; }
                else { color = m[x, y]; run = 1; }
            }
        }

        // Rule 2: 2x2 same-color blocks
        for (int y = 0; y < size - 1; y++)
            for (int x = 0; x < size - 1; x++)
                if (m[x, y] == m[x + 1, y] && m[x, y] == m[x, y + 1] && m[x, y] == m[x + 1, y + 1])
                    penalty += 3;

        // Rule 3: finder-like patterns (1011101 + 4 light modules adjacent)
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size - 6; x++)
                if (FinderLike(m, x, y, 1, 0)) penalty += 40;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size - 6; y++)
                if (FinderLike(m, x, y, 0, 1)) penalty += 40;

        // Rule 4: dark/light balance
        int dark = 0;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (m[x, y]) dark++;
        int total = size * size;
        int pctDev = Math.Abs(dark * 20 - total * 10) / total;
        penalty += pctDev * 10;
        return penalty;
    }

    private static readonly bool[] FinderPattern = { true, false, true, true, true, false, true };

    private static bool FinderLike(bool[,] m, int x, int y, int dx, int dy)
    {
        for (int i = 0; i < 7; i++)
            if (m[x + dx * i, y + dy * i] != FinderPattern[i]) return false;
        return true;
    }

    // ============================================================
    // Bit buffer
    // ============================================================

    private sealed class BitBuffer
    {
        private readonly List<bool> _bits = new();
        public int Length => _bits.Count;
        public void AppendBits(int value, int len)
        {
            if (len < 0 || len > 31) throw new ArgumentOutOfRangeException(nameof(len));
            for (int i = len - 1; i >= 0; i--) _bits.Add(((value >> i) & 1) != 0);
        }
        public byte[] ToBytes()
        {
            var bytes = new byte[(_bits.Count + 7) / 8];
            for (int i = 0; i < _bits.Count; i++)
                if (_bits[i]) bytes[i >> 3] |= (byte)(1 << (7 - (i & 7)));
            return bytes;
        }
    }
}
