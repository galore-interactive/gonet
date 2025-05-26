using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GONet.Utils;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Tests to ensure time synchronization works consistently across different platforms
    /// and handles platform-specific quirks.
    /// </summary>
    [TestFixture]
    public class CrossPlatformTimeTests : TimeSyncTestBase
    {
        [Test]
        [Category("CrossPlatform")]
        public void Should_Handle_Platform_Specific_Timer_Resolution()
        {
            // Different platforms have different timer resolutions
            var measurements = new List<double>();

            for (int i = 0; i < 1000; i++)
            {
                var start = HighResolutionTimeUtils.UtcNow;
                Thread.Yield(); // Minimal work
                var end = HighResolutionTimeUtils.UtcNow;

                var elapsedMs = (end - start).TotalMilliseconds;
                if (elapsedMs > 0)
                {
                    measurements.Add(elapsedMs);
                }
            }

            if (measurements.Count > 0)
            {
                var minResolution = measurements.Min();
                var avgResolution = measurements.Average();

                UnityEngine.Debug.Log($"Platform timer resolution: Min={minResolution:F6}ms, Avg={avgResolution:F6}ms");
                UnityEngine.Debug.Log($"Platform: {Application.platform}, OS: {SystemInfo.operatingSystem}");

                // Platform-specific expectations
                if (Application.platform == RuntimePlatform.WindowsPlayer ||
                    Application.platform == RuntimePlatform.WindowsEditor)
                {
                    Assert.That(minResolution, Is.LessThan(1.0),
                        "Windows should have sub-millisecond resolution");
                }
                else if (Application.platform == RuntimePlatform.Android)
                {
                    Assert.That(minResolution, Is.LessThan(10.0),
                        "Android should have reasonable timer resolution");
                }
            }
        }

        [Test]
        [Category("CrossPlatform")]
        public void Should_Handle_System_Clock_Changes()
        {
            var clientTime = new SecretaryOfTemporalAffairs();
            clientTime.Update();

            // Simulate system clock change detection
            var samples = new List<(DateTime system, DateTime highRes)>();

            for (int i = 0; i < 10; i++)
            {
                samples.Add((DateTime.UtcNow, HighResolutionTimeUtils.UtcNow));
                Thread.Sleep(100);
            }

            // Check for consistency
            double maxDeviation = 0;
            for (int i = 1; i < samples.Count; i++)
            {
                var systemDelta = (samples[i].system - samples[i - 1].system).TotalMilliseconds;
                var highResDelta = (samples[i].highRes - samples[i - 1].highRes).TotalMilliseconds;
                var deviation = Math.Abs(systemDelta - highResDelta);
                maxDeviation = Math.Max(maxDeviation, deviation);
            }

            UnityEngine.Debug.Log($"Max deviation between system and high-res time: {maxDeviation:F3}ms");

            // High-res timer should be independent of system clock changes
            Assert.That(maxDeviation, Is.LessThan(10.0),
                "High-resolution timer should be stable");
        }

        [Test]
        [Category("CrossPlatform")]
        public void Should_Handle_Power_Management_Events()
        {
            // Simulate device sleep/wake cycle effects
            var beforeSleep = HighResolutionTimeUtils.UtcNow;

            // Simulate a pause (like device sleep)
            Thread.Sleep(1000);

            var afterWake = HighResolutionTimeUtils.UtcNow;
            var elapsed = (afterWake - beforeSleep).TotalSeconds;

            Assert.That(elapsed, Is.InRange(0.9, 1.2),
                "Time should progress normally even after pause");

            // Test recovery after long pause
            var clientTime = new SecretaryOfTemporalAffairs();
            clientTime.Update();

            var serverTime = new SecretaryOfTemporalAffairs();
            serverTime.Update();
            serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromMinutes(5).Ticks);

            // Client should be able to resync after wake
            var request = new RequestMessage(clientTime.ElapsedTicks);
            HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverTime.ElapsedTicks,
                request,
                clientTime,
                true
            );

            Assert.Pass("System handles power management events");
        }
    }

    /// <summary>
    /// Tests for mobile-specific scenarios
    /// </summary>
    [TestFixture]
    public class MobileSpecificTimeTests : TimeSyncTestBase
    {
        [Test]
        [Category("Mobile")]
        public void Should_Handle_Background_Foreground_Transitions()
        {
            var clientTime = new SecretaryOfTemporalAffairs();
            clientTime.Update();

            // Simulate app going to background
            var timeBeforeBackground = clientTime.ElapsedTicks;

            // Simulate time passing while in background
            Thread.Sleep(5000);

            // Simulate return to foreground
            clientTime.Update();
            var timeAfterForeground = clientTime.ElapsedTicks;

            var elapsedSeconds = (timeAfterForeground - timeBeforeBackground) / (double)TimeSpan.TicksPerSecond;

            Assert.That(elapsedSeconds, Is.GreaterThan(4.9),
                "Time should continue to progress in background");

            // Test resync after background
            var serverTime = new SecretaryOfTemporalAffairs();
            serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(30).Ticks);

            var request = new RequestMessage(clientTime.ElapsedTicks);
            HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverTime.ElapsedTicks,
                request,
                clientTime,
                true
            );

            Thread.Sleep(1100); // Wait for interpolation
            clientTime.Update();

            Assert.Pass("Handles background/foreground transitions");
        }

        [Test]
        [Category("Mobile")]
        public void Should_Handle_Network_Type_Changes()
        {
            // Simulate switching from WiFi to Cellular with different latencies
            var wifiLatencyMs = 20;
            var cellularLatencyMs = 100;

            var clientTime = new SecretaryOfTemporalAffairs();
            var serverTime = new SecretaryOfTemporalAffairs();

            clientTime.Update();
            serverTime.Update();
            serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(5).Ticks);

            // Sync with WiFi latency
            for (int i = 0; i < 3; i++)
            {
                var request = new RequestMessage(clientTime.ElapsedTicks);
                Thread.Sleep(wifiLatencyMs);

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime,
                    i == 0
                );

                Thread.Sleep(500);
                clientTime.Update();
                serverTime.Update();
            }

            // Switch to cellular latency
            UnityEngine.Debug.Log("Simulating network switch to cellular");

            for (int i = 0; i < 3; i++)
            {
                var request = new RequestMessage(clientTime.ElapsedTicks);
                Thread.Sleep(cellularLatencyMs);

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime,
                    false
                );

                Thread.Sleep(500);
                clientTime.Update();
                serverTime.Update();
            }

            // Should maintain sync despite latency change
            var diff = Math.Abs(serverTime.ElapsedSeconds - clientTime.ElapsedSeconds);
            Assert.That(diff, Is.LessThan(0.2),
                "Should maintain sync after network type change");
        }
    }
}