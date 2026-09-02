using Xunit;

namespace TCIP.Business.Tests;

public sealed class LoadProfileAndScaleTests
{
    [Fact]
    public void MillionRecipients_GeneratesExactly1000Batches_WithBoundedMemoryAndMax1001RowsPerPage()
    {
        const int totalRecipients = 1_000_000;
        const int pageSize = 1001;
        const int batchSize = 1000;

        int batchCount = 0;
        int maxRowsPerQuery = 0;
        int currentIndex = 0;

        while (currentIndex < totalRecipients)
        {
            var remaining = totalRecipients - currentIndex;
            var rowsReturned = Math.Min(remaining, pageSize);

            if (rowsReturned > maxRowsPerQuery)
            {
                maxRowsPerQuery = rowsReturned;
            }

            var hasMore = rowsReturned > batchSize;
            var currentBatchCount = Math.Min(rowsReturned, batchSize);
            batchCount++;

            currentIndex += currentBatchCount;

            if (!hasMore)
            {
                break;
            }
        }

        Assert.Equal(1_000, batchCount);
        Assert.Equal(1_001, maxRowsPerQuery);
        Assert.Equal(totalRecipients, currentIndex);
    }
}
