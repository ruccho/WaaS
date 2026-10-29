using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.Tests;

public class MemoryTests
{
    [Test]
    public void TestGrowPreservesContentsAndZeroesNewPages()
    {
        const int pageSize = 1 << Memory.PageSizeRank;

        // leave a dirty buffer in the pool so that a reused buffer is detected
        using (var dirty = new Memory(new Limits(4)))
        {
            dirty.Span.Fill(0xFF);
        }

        using var memory = new Memory(new Limits(1));
        memory.Span[0] = 1;

        for (var i = 1; i < 20; i++)
        {
            Assert.That(memory.TryGrow(1), Is.True);
            Assert.That(memory.NumPages, Is.EqualTo(i + 1));
            Assert.That(memory.Length, Is.EqualTo((i + 1) * pageSize));

            var newPage = memory.Span.Slice(i * pageSize, pageSize);
            Assert.That(newPage.IndexOfAnyExcept((byte)0), Is.EqualTo(-1), $"page {i} is not zeroed");
            newPage[0] = (byte)(i + 1);
        }

        for (var i = 0; i < 20; i++)
            Assert.That(memory.Span[i * pageSize], Is.EqualTo((byte)(i + 1)), $"page {i} is not preserved");
    }

    [Test]
    public void TestGrowBeyondMaximum()
    {
        using var memory = new Memory(new Limits(1, 2));
        Assert.That(memory.TryGrow(1), Is.True);
        Assert.That(memory.TryGrow(1), Is.False);
        Assert.That(memory.NumPages, Is.EqualTo(2));
    }
}
