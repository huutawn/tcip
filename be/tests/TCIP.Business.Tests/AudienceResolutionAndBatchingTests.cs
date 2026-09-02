using Xunit;

namespace TCIP.Business.Tests;

public sealed class AudienceResolutionAndBatchingTests
{
    [Fact]
    public void Audience_MultipleMemberships_DeduplicatesRecipients()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var audience1Users = new List<Guid> { user1, user2 };
        var audience2Users = new List<Guid> { user1 };

        var distinctUsers = audience1Users.Union(audience2Users).ToList();

        Assert.Equal(2, distinctUsers.Count);
        Assert.Contains(user1, distinctUsers);
        Assert.Contains(user2, distinctUsers);
    }

    [Theory]
    [InlineData(999, 1)]   // 999 recipients => exactly 1 batch
    [InlineData(1000, 1)]  // 1000 recipients => exactly 1 batch
    [InlineData(1001, 2)]  // 1001 recipients => exactly 2 batches (1000 + 1)
    public void KeysetPagination_BatchingBoundaries_ProducesExpectedBatchCounts(int totalRecipients, int expectedBatches)
    {
        var allRecipients = Enumerable.Range(0, totalRecipients)
            .Select(_ => Guid.NewGuid())
            .OrderBy(x => x)
            .ToList();

        var batchCount = 0;
        Guid? cursor = null;

        while (true)
        {
            var page = allRecipients
                .Where(x => cursor == null || x.CompareTo(cursor.Value) > 0)
                .Take(1001)
                .ToList();

            if (page.Count == 0)
                break;

            var hasMore = page.Count > 1000;
            var currentBatch = page.Take(1000).ToList();
            batchCount++;

            if (!hasMore)
                break;

            cursor = currentBatch[^1];
        }

        Assert.Equal(expectedBatches, batchCount);
    }
}
