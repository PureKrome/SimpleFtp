using System;
using Shouldly;

namespace SimpleFtp
{
    public class UploadProgressArgs : EventArgs
    {
        public UploadProgressArgs(int eventId,
                                  long totalBytesUploaded,
                                  long currentBytesUploaded,
                                  long totalSourceBytes)
        {
            eventId.ShouldBeGreaterThan(0);
            totalBytesUploaded.ShouldBeGreaterThan(0);
            currentBytesUploaded.ShouldBeGreaterThan(0);

            EventId = eventId;
            TotalBytesUploaded = totalBytesUploaded;
            CurrentBytesUploaded = currentBytesUploaded;

            PercentageUploaded = (double) totalBytesUploaded/(double) totalSourceBytes*100.0;
        }

        public int EventId { get; }
        public long TotalBytesUploaded { get; }
        public long CurrentBytesUploaded { get; }
        public double PercentageUploaded { get; }
    }
}