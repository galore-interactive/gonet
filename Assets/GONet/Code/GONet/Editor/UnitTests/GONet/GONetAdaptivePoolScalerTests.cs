/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 *
 *
 * Authorized use is explicitly limited to the following:
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using NUnit.Framework;
using UnityEngine;

namespace GONet.Editor.UnitTests
{
    [TestFixture]
    public class GONetAdaptivePoolScalerTests
    {
        private GameObject testGameObject;
        private GONetGlobal testConfig;
        private float currentTime; // Simulated time for tests

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with GONetGlobal component
            testGameObject = new GameObject("TestGONetGlobal");
            testConfig = testGameObject.AddComponent<GONetGlobal>();

            // Set default test values
            testConfig.enableAdaptivePoolScaling = true;
            testConfig.adaptivePoolBaselineSize = 1000;
            testConfig.maxPacketsPerTick = 20000;

            // Initialize simulated time
            currentTime = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void Constructor_InitializesWithBaselineSize_WhenAdaptiveEnabled()
        {
            // Arrange & Act
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // Assert
            Assert.AreEqual(1000, scaler.GetCurrentPoolSize(), "Initial pool size should equal baseline when adaptive enabled");
        }

        [Test]
        public void Constructor_InitializesWithMaxSize_WhenAdaptiveDisabled()
        {
            // Arrange
            testConfig.enableAdaptivePoolScaling = false;

            // Act
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // Assert
            Assert.AreEqual(20000, scaler.GetCurrentPoolSize(), "Initial pool size should equal max when adaptive disabled");
        }

        [Test]
        public void Constructor_ClampsBaseline_WhenBaselineExceedsMax()
        {
            // Arrange
            testConfig.adaptivePoolBaselineSize = 25000; // Higher than max
            testConfig.maxPacketsPerTick = 20000;

            // Act
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // Assert
            Assert.AreEqual(20000, scaler.GetCurrentPoolSize(), "Baseline should be clamped to max when exceeding it");
        }

        [Test]
        public void Update_ScalesUp_WhenUtilizationExceeds75Percent()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int initialSize = scaler.GetCurrentPoolSize(); // 1000
            int highUtilization = (int)(initialSize * 0.76); // 76% - above 75% threshold

            // Act - Simulate high utilization for 1+ second
            SimulateTimePassage(1.1f);
            scaler.Update(highUtilization, numConnectedClients: 5, currentTime);

            // Assert
            int newSize = scaler.GetCurrentPoolSize();
            Assert.Greater(newSize, initialSize, "Pool should scale up when utilization >75%");
            Assert.AreEqual((int)(initialSize * 1.5), newSize, "Pool should grow by 1.5x factor");
        }

        [Test]
        public void Update_DoesNotScaleUp_WhenUtilizationBelow75Percent()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int initialSize = scaler.GetCurrentPoolSize(); // 1000
            int lowUtilization = (int)(initialSize * 0.74); // 74% - below threshold

            // Act
            SimulateTimePassage(1.1f);
            scaler.Update(lowUtilization, numConnectedClients: 5, currentTime);

            // Assert
            Assert.AreEqual(initialSize, scaler.GetCurrentPoolSize(), "Pool should NOT scale up when utilization <75%");
        }

        [Test]
        public void Update_ScalesDown_WhenUtilizationBelow25PercentFor5Seconds()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // First scale up
            SimulateTimePassage(1.1f);
            scaler.Update((int)(1000 * 0.76), numConnectedClients: 5, currentTime); // Scale to 1500
            int scaledUpSize = scaler.GetCurrentPoolSize();
            Assert.Greater(scaledUpSize, 1000, "Precondition: Should have scaled up");

            // Act - Low utilization for 5+ seconds
            int lowUtilization = (int)(scaledUpSize * 0.24); // 24% - below 25%
            for (int i = 0; i < 6; i++) // 6 seconds of low utilization
            {
                SimulateTimePassage(1.1f);
                scaler.Update(lowUtilization, numConnectedClients: 5, currentTime);
            }

            // Assert
            Assert.AreEqual(1000, scaler.GetCurrentPoolSize(), "Pool should scale down to baseline after 5s of low utilization");
        }

        [Test]
        public void Update_DoesNotScaleDown_WhenUtilizationLowButNotFor5Seconds()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // First scale up
            SimulateTimePassage(1.1f);
            scaler.Update((int)(1000 * 0.76), numConnectedClients: 5, currentTime);
            int scaledUpSize = scaler.GetCurrentPoolSize();

            // Act - Low utilization for only 3 seconds
            int lowUtilization = (int)(scaledUpSize * 0.24);
            for (int i = 0; i < 3; i++)
            {
                SimulateTimePassage(1.1f);
                scaler.Update(lowUtilization, numConnectedClients: 5, currentTime);
            }

            // Assert
            Assert.AreEqual(scaledUpSize, scaler.GetCurrentPoolSize(), "Pool should NOT scale down before 5s delay");
        }

        [Test]
        public void Update_RespectsMaxCeiling_WhenScalingUp()
        {
            // Arrange
            testConfig.maxPacketsPerTick = 2000; // Low ceiling
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // Act - Repeatedly try to scale up beyond ceiling
            for (int i = 0; i < 10; i++)
            {
                SimulateTimePassage(1.1f);
                int currentSize = scaler.GetCurrentPoolSize();
                scaler.Update((int)(currentSize * 0.90), numConnectedClients: 5, currentTime); // Always 90% utilized
            }

            // Assert
            Assert.LessOrEqual(scaler.GetCurrentPoolSize(), 2000, "Pool should never exceed max ceiling");
        }

        [Test]
        public void Update_DoesNotScale_WhenAdaptiveDisabled()
        {
            // Arrange
            testConfig.enableAdaptivePoolScaling = false;
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int fixedSize = scaler.GetCurrentPoolSize(); // Should be maxPacketsPerTick

            // Act - Try to trigger scale-up
            SimulateTimePassage(1.1f);
            scaler.Update((int)(fixedSize * 0.90), numConnectedClients: 5, currentTime);

            // Assert
            Assert.AreEqual(fixedSize, scaler.GetCurrentPoolSize(), "Pool should remain fixed when adaptive disabled");
        }

        [Test]
        public void Update_UsesOnlyUpdateInterval_NotEveryCall()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int initialSize = scaler.GetCurrentPoolSize();

            // Act - Multiple updates within 1 second (should be ignored)
            for (int i = 0; i < 5; i++)
            {
                SimulateTimePassage(0.1f); // 100ms each = 500ms total
                scaler.Update((int)(initialSize * 0.90), numConnectedClients: 5, currentTime);
            }

            // Assert
            Assert.AreEqual(initialSize, scaler.GetCurrentPoolSize(), "Pool should not update when <1s has passed");
        }

        [Test]
        public void Update_TracksPeakUtilization_BetweenUpdates()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int initialSize = scaler.GetCurrentPoolSize();

            // Act - Multiple calls with varying utilization, peak exceeds threshold
            SimulateTimePassage(0.3f);
            scaler.Update(100, numConnectedClients: 5, currentTime);  // Low

            SimulateTimePassage(0.3f);
            scaler.Update((int)(initialSize * 0.80), numConnectedClients: 5, currentTime);  // PEAK - 80%!

            SimulateTimePassage(0.5f);  // Total > 1s, will trigger check
            scaler.Update(200, numConnectedClients: 5, currentTime);  // Low again

            // Assert
            Assert.Greater(scaler.GetCurrentPoolSize(), initialSize,
                "Pool should scale based on PEAK utilization (80%), not final utilization (200)");
        }

        [Test]
        public void Update_GrowsByMinimumIncrement_WhenCalculatedGrowthTooSmall()
        {
            // Arrange
            testConfig.adaptivePoolBaselineSize = 100; // Very small baseline
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // Act - Scale up from 100
            SimulateTimePassage(1.1f);
            scaler.Update(80, numConnectedClients: 1, currentTime); // 80% utilization

            // Assert
            int newSize = scaler.GetCurrentPoolSize();
            int expectedMinimum = 100 + 100; // baseline + minimum increment (100)
            Assert.GreaterOrEqual(newSize, expectedMinimum,
                "Pool should grow by at least minimum increment (100) even when 1.5x factor is smaller");
        }

        [Test]
        public void RefreshConfiguration_UpdatesSettings_WhenConfigChanged()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int originalSize = scaler.GetCurrentPoolSize();

            // Act - Change config and refresh
            testConfig.enableAdaptivePoolScaling = false;
            scaler.RefreshConfiguration(testConfig);

            SimulateTimePassage(1.1f);
            scaler.Update(500, numConnectedClients: 5, currentTime);

            // Assert
            Assert.AreEqual(testConfig.maxPacketsPerTick, scaler.GetCurrentPoolSize(),
                "Pool should use fixed size after refresh when adaptive disabled");
        }

        [Test]
        public void GetDiagnostics_ReturnsFormattedString_WithCorrectValues()
        {
            // Arrange
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);

            // Act
            string diagnostics = scaler.GetDiagnostics();

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Contains("ADAPTIVE"), "Diagnostics should show mode");
            Assert.IsTrue(diagnostics.Contains("1000"), "Diagnostics should show baseline");
            Assert.IsTrue(diagnostics.Contains("20000"), "Diagnostics should show max");
        }

        [Test]
        public void ScalingSequence_RealisticSpawnBurst_HandlesCorrectly()
        {
            // Arrange - Simulate realistic spawn burst scenario
            var scaler = new GONetAdaptivePoolScaler(testConfig, currentTime);
            int[] utilizationPattern = { 100, 500, 800, 1200, 900, 400, 100 }; // Burst then calm
            int currentSize = scaler.GetCurrentPoolSize();

            // Act & Assert - Step through burst
            foreach (int utilization in utilizationPattern)
            {
                SimulateTimePassage(1.1f);
                scaler.Update(utilization, numConnectedClients: 8, currentTime);

                currentSize = scaler.GetCurrentPoolSize();

                // Verify pool never drops packets
                float utilizationPercent = (float)utilization / currentSize;
                Assert.Less(utilizationPercent, 0.90f,
                    $"Utilization {utilizationPercent:P} should stay below drop threshold after scaling");
            }

            // Final size should have scaled up from initial 1000
            Assert.Greater(currentSize, 1000, "Pool should have scaled up during burst");
        }

        // ========================================
        // Helper Methods
        // ========================================

        private void SimulateTimePassage(float seconds)
        {
            // Advance simulated time
            currentTime += seconds;
        }
    }
}
