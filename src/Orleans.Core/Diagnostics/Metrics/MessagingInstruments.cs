using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Orleans.Messaging;

namespace Orleans.Runtime
{
    internal static class MessagingInstruments
    {
        internal static readonly Counter<int> HeaderBytesSentCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_SENT_BYTES_HEADER);
        internal static readonly Counter<int> HeaderBytesReceivedCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_RECEIVED_BYTES_HEADER);
        internal static readonly Counter<int> LocalMessagesSentCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_SENT_LOCALMESSAGES);
        internal static readonly Counter<int> FailedSentMessagesCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_SENT_FAILED);
        internal static readonly Counter<int> DroppedSentMessagesCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_SENT_DROPPED);
        internal static readonly Counter<int> RejectedMessagesCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_REJECTED);
        internal static readonly Counter<int> ReroutedMessagesCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_REROUTED);
        internal static readonly Counter<int> ExpiredMessagesCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_EXPIRED);

        // ! UpDownCounter
        internal static readonly Counter<int> ConnectedClient = Instruments.Meter.CreateCounter<int>(StatisticNames.GATEWAY_CONNECTED_CLIENTS);
        internal static readonly Counter<int> PingSendCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_PINGS_SENT);
        internal static readonly Counter<int> PingReceivedCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_PINGS_RECEIVED);
        internal static readonly Counter<int> PingReplyReceivedCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_PINGS_REPLYRECEIVED);
        internal static readonly Counter<int> PingReplyMissedCounter = Instruments.Meter.CreateCounter<int>(StatisticNames.MESSAGING_PINGS_REPLYMISSED);
        internal static readonly Histogram<int> MessageSentSizeHistogram = Instruments.Meter.CreateHistogram<int>(StatisticNames.MESSAGING_SENT_MESSAGES_SIZE, "bytes");
        internal static readonly Histogram<int> MessageReceivedSizeHistogram = Instruments.Meter.CreateHistogram<int>(StatisticNames.MESSAGING_RECEIVED_MESSAGES_SIZE, "bytes");


        internal enum Phase
        {
            Send,
            Receive,
            Dispatch,
            Invoke,
            Respond,
        }

        // TODO: bucket size need to be configured at collector side
        // [Add "hints" in Metric API to provide things like histogram bounds]
        // https://github.com/dotnet/runtime/issues/63650
        // Histogram of sent  message size, starting from 0 in multiples of 2
        // (1=2^0, 2=2^2, ... , 256=2^8, 512=2^9, 1024==2^10, ... , up to ... 2^30=1GB)
        // private const int NUM_MSG_SIZE_HISTOGRAM_CATEGORIES = 31;

        internal static void OnMessageExpired(Phase phase)
        {
            ExpiredMessagesCounter.Add(1, new KeyValuePair<string, object>("Phase", phase));
        }

        internal static void OnPingSend(SiloAddress destination)
        {
            PingSendCounter.Add(1, new KeyValuePair<string, object>("Destination", destination.ToString()));
        }

        internal static void OnPingReceive(SiloAddress destination)
        {
            PingReceivedCounter.Add(1, new KeyValuePair<string, object>("Destination", destination.ToString()));
        }

        internal static void OnPingReplyReceived(SiloAddress replier)
        {
            PingReplyReceivedCounter.Add(1, new KeyValuePair<string, object>("Destination", replier.ToString()));
        }

        internal static void OnPingReplyMissed(SiloAddress replier)
        {
            PingReplyMissedCounter.Add(1, new KeyValuePair<string, object>("Destination", replier.ToString()));
        }

        internal static void OnFailedSentMessage(Message msg)
        {
            if (msg == null || !msg.HasDirection) return;
            FailedSentMessagesCounter.Add(1, new KeyValuePair<string, object>("Direction", msg.Direction));
        }

        internal static void OnDroppedSentMessage(Message msg)
        {
            if (msg == null || !msg.HasDirection) return;
            DroppedSentMessagesCounter.Add(1, new KeyValuePair<string, object>("Direction", msg.Direction));
        }

        internal static void OnRejectedMessage(Message msg)
        {
            if (msg == null || !msg.HasDirection) return;
            RejectedMessagesCounter.Add(1, new KeyValuePair<string, object>("Direction", msg.Direction));
        }

        internal static void OnMessageReRoute(Message msg)
        {
            ReroutedMessagesCounter.Add(1, new KeyValuePair<string, object>("Direction", msg.Direction));
        }

        internal static void OnMessageReceive(Message msg, int numTotalBytes, int headerBytes, ConnectionDirection connectionDirection, SiloAddress remoteSiloAddress = null)
        {
            if (remoteSiloAddress != null)
            {
                MessageReceivedSizeHistogram.Record(numTotalBytes, new KeyValuePair<string, object>("ConnectionDirection", connectionDirection), new KeyValuePair<string, object>("MessageDirection", msg.Direction), new KeyValuePair<string, object>("silo", remoteSiloAddress));
            }
            else
            {
                MessageReceivedSizeHistogram.Record(numTotalBytes, new KeyValuePair<string, object>("ConnectionDirection", connectionDirection), new KeyValuePair<string, object>("MessageDirection", msg.Direction));
            }
            HeaderBytesReceivedCounter.Add(headerBytes);
        }
        internal static void OnMessageSend(Message msg, int numTotalBytes, int headerBytes, ConnectionDirection connectionDirection, SiloAddress remoteSiloAddress = null)
        {
            Debug.Assert(numTotalBytes >= 0, $"OnMessageSend(numTotalBytes={numTotalBytes})");

            if (remoteSiloAddress != null)
            {
                MessageSentSizeHistogram.Record(numTotalBytes, new KeyValuePair<string, object>("ConnectionDirection", connectionDirection), new KeyValuePair<string, object>("MessageDirection", msg.Direction), new KeyValuePair<string, object>("silo", remoteSiloAddress));
            }
            else
            {
                MessageSentSizeHistogram.Record(numTotalBytes, new KeyValuePair<string, object>("ConnectionDirection", connectionDirection), new KeyValuePair<string, object>("MessageDirection", msg.Direction));
            }
            HeaderBytesSentCounter.Add(headerBytes);
        }
    }
}
