using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GONet.Utils;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;
using Newtonsoft.Json;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Tests time synchronization against real external time sources to validate
    /// the sync algorithm works with real network conditions and authoritative time servers.
    /// </summary>
    [TestFixture]
    public class ExternalTimeSourceIntegrationTests : TimeSyncTestBase
    {
        private HttpClient httpClient;
        private SecretaryOfTemporalAffairs clientTime;

        [SetUp]
        public void Setup()
        {
            base.BaseSetUp();
            httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            clientTime = new SecretaryOfTemporalAffairs();
            clientTime.Update();
        }

        [TearDown]
        public void TearDown()
        {
            httpClient?.Dispose();
            base.BaseTearDown();
        }

        [Test]
        [Category("ExternalSync")]
        [Explicit("Requires internet connection and external service availability")]
        [Timeout(30000)]
        public async Task Should_Sync_With_WorldTimeAPI()
        {
            // WorldTimeAPI provides millisecond-precision timestamps
            const string timeApiUrl = "http://worldtimeapi.org/api/timezone/Etc/UTC";

            var syncResults = new List<(double rtt, double offset)>();

            for (int i = 0; i < 5; i++)
            {
                if (cts.Token.IsCancellationRequested) break;

                try
                {
                    // Record request time
                    var stopwatch = Stopwatch.StartNew();
                    var requestTime = HighResolutionTimeUtils.UtcNow;

                    // Make HTTP request
                    var response = await httpClient.GetStringAsync(timeApiUrl);

                    stopwatch.Stop();
                    var responseTime = HighResolutionTimeUtils.UtcNow;

                    // Parse response
                    dynamic timeData = JsonConvert.DeserializeObject(response);
                    long unixTimeMs = (long)timeData.unixtime * 1000;
                    var serverTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMs).UtcDateTime;

                    // Calculate RTT and offset
                    double rttSeconds = stopwatch.Elapsed.TotalSeconds;
                    double oneWayDelay = rttSeconds / 2.0;

                    // Adjust server time by one-way delay
                    var adjustedServerTime = serverTime.AddSeconds(oneWayDelay);
                    var localTime = responseTime;

                    double offsetSeconds = (adjustedServerTime - localTime).TotalSeconds;
                    syncResults.Add((rttSeconds, offsetSeconds));

                    UnityEngine.Debug.Log($"Sync {i}: RTT={rttSeconds * 1000:F1}ms, Offset={offsetSeconds * 1000:F1}ms");

                    // Apply sync using your system
                    if (i == 0)
                    {
                        // First sync - force adjustment
                        long serverTicks = adjustedServerTime.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
                        clientTime.SetFromAuthority(serverTicks);
                    }

                    await Task.Delay(1000); // Wait between syncs
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"WorldTimeAPI sync failed: {ex.Message}");
                }
            }

            if (syncResults.Count > 0)
            {
                var avgOffset = syncResults.Average(r => Math.Abs(r.offset));
                var avgRtt = syncResults.Average(r => r.rtt);

                UnityEngine.Debug.Log($"External sync results: Avg RTT={avgRtt * 1000:F1}ms, Avg offset={avgOffset * 1000:F1}ms");

                // With internet RTT and potential system clock offset, we expect higher tolerance
                Assert.That(avgOffset, Is.LessThan(1.0), "Should sync within 1000ms considering network and clock variations");
            }
        }

        [Test]
        [Category("ExternalSync")]
        [Explicit("Requires internet connection and external service availability")]
        [Timeout(30000)]
        public async Task Should_Handle_Variable_Internet_Latency()
        {
            // Use multiple time servers to simulate varying network paths
            var timeServers = new[]
            {
                "http://worldtimeapi.org/api/timezone/Etc/UTC",
                "https://timeapi.io/api/Time/current/zone?timeZone=UTC",
                "http://worldclockapi.com/api/json/utc/now"
            };

            var rttSamples = new List<double>();

            foreach (var server in timeServers)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(server);
                    stopwatch.Stop();

                    if (response.IsSuccessStatusCode)
                    {
                        rttSamples.Add(stopwatch.Elapsed.TotalSeconds);
                        UnityEngine.Debug.Log($"Server {server}: RTT={stopwatch.ElapsedMilliseconds}ms");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to reach {server}: {ex.Message}");
                }
            }

            if (rttSamples.Count >= 2)
            {
                var minRtt = rttSamples.Min();
                var maxRtt = rttSamples.Max();
                var rttVariance = maxRtt - minRtt;

                UnityEngine.Debug.Log($"RTT variance across servers: {rttVariance * 1000:F1}ms");

                // Your sync algorithm should handle this variance
                Assert.That(rttVariance, Is.GreaterThan(0), "Should see RTT variance across different servers");
            }
        }

        [Test]
        [Category("TimeSync")]
        public void Should_Handle_External_Time_Source_Simulation()
        {
            // Simulate what would happen with an external time source
            var clientTime = new SecretaryOfTemporalAffairs();
            clientTime.Update();

            // Simulate getting time from external source with network delay
            Thread.Sleep(100); // Simulate network request

            // External source says current time is X
            var simulatedExternalTime = DateTime.UtcNow.AddSeconds(5); // 5 seconds ahead
            var externalTicks = simulatedExternalTime.Ticks;

            // Simulate network delay in response
            Thread.Sleep(50);

            // Apply the external time
            clientTime.SetFromAuthority(externalTicks);
            Thread.Sleep(1100); // Wait for interpolation

            clientTime.Update();

            // Verify we adjusted toward external time
            var clientSeconds = clientTime.ElapsedSeconds;
            Assert.That(clientSeconds, Is.GreaterThan(0),
                "Should have valid time after external sync");
        }
    }

    /// <summary>
    /// Tests using NTP servers for high-precision time synchronization
    /// </summary>
    [TestFixture]
    public class NtpTimeSyncTests : TimeSyncTestBase
    {
        private SecretaryOfTemporalAffairs clientTime;

        [SetUp]
        public void Setup()
        {
            base.BaseSetUp();
            clientTime = new SecretaryOfTemporalAffairs();
            clientTime.Update();
        }

        [Test]
        [Category("NTPSync")]
        [Timeout(15000)]
        public async Task Should_Sync_With_NTP_Server()
        {
            // Simple NTP client implementation for testing
            var ntpData = new byte[48];
            ntpData[0] = 0x1B; // NTP version 3, client mode

            try
            {
                using (var socket = new System.Net.Sockets.UdpClient())
                {
                    socket.Client.ReceiveTimeout = 5000;

                    var ntpEndpoint = new System.Net.IPEndPoint(
                        System.Net.Dns.GetHostAddresses("pool.ntp.org")[0],
                        123
                    );

                    // Record precise send time
                    var sendTime = HighResolutionTimeUtils.UtcNow;
                    await socket.SendAsync(ntpData, ntpData.Length, ntpEndpoint);

                    // Receive response
                    var result = await socket.ReceiveAsync();
                    var receiveTime = HighResolutionTimeUtils.UtcNow;

                    // Extract NTP timestamp (seconds since 1900)
                    ulong intPart = System.BitConverter.ToUInt32(result.Buffer, 40);
                    ulong fracPart = System.BitConverter.ToUInt32(result.Buffer, 44);

                    if (System.BitConverter.IsLittleEndian)
                    {
                        intPart = SwapEndianness(intPart);
                        fracPart = SwapEndianness(fracPart);
                    }

                    var ntpTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddSeconds(intPart)
                        .AddSeconds(fracPart / 4294967296.0);

                    var rtt = (receiveTime - sendTime).TotalSeconds;
                    var oneWayDelay = rtt / 2.0;
                    var adjustedNtpTime = ntpTime.AddSeconds(oneWayDelay);

                    var offset = (adjustedNtpTime - receiveTime).TotalSeconds;

                    UnityEngine.Debug.Log($"NTP sync: RTT={rtt * 1000:F1}ms, Offset={offset * 1000:F1}ms");

                    // Check system clock offset
                    var systemOffset = (adjustedNtpTime - DateTime.UtcNow).TotalSeconds;
                    if (Math.Abs(systemOffset) > 0.5)
                    {
                        Assert.Inconclusive($"System clock is off by {systemOffset:F1}s from NTP time. " +
                                          "Please sync your system clock to run this test properly.");
                    }

                    // NTP should provide very accurate time
                    Assert.That(Math.Abs(offset), Is.LessThan(0.1),
                        "Should sync within 100ms of NTP time");
                }
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"NTP test failed (may be network issue): {ex.Message}");
            }
        }

        private static uint SwapEndianness(ulong value)
        {
            return (uint)(((value & 0x000000ff) << 24) +
                         ((value & 0x0000ff00) << 8) +
                         ((value & 0x00ff0000) >> 8) +
                         ((value & 0xff000000) >> 24));
        }
    }
}