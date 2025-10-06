using GONet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Sample
{
    /// <summary>
    /// Executes GONetTestScript steps with full orchestration logic.
    /// This executor runs on the server and coordinates the entire test.
    /// </summary>
    public class GONetTestScriptExecutor : GONetParticipantCompanionBehaviour
    {
        [Header("Test Script")]
        [Tooltip("Test script asset (TextAsset with .gotest content)")]
        public TextAsset testScriptAsset;

        [Header("Runtime State")]
        public bool isExecuting = false;
        public int currentStepIndex = -1;
        public string currentStepDescription = "";

        private GONetTestScript script;
        private List<uint> trackedBeaconIds = new List<uint>();
        private HashSet<ushort> connectedClients = new HashSet<ushort>();
        private TestInstructionUI instructionUI;
        private TestSelectionUI selectionUI;
        private GONetTestLogger testLogger;
        private List<TestResult> testResults = new List<TestResult>();
        private bool hasInitialized = false;

        // Track expected spawns per authority to detect RPC delivery failures
        private Dictionary<ushort, int> expectedSpawnsByAuthority = new Dictionary<ushort, int>();
        private Dictionary<ushort, List<uint>> actualSpawnsByAuthority = new Dictionary<ushort, List<uint>>();

        protected override void Start()
        {
            base.Start();

            if (!GONetMain.IsServer)
                return;

            // Create test selection UI instead of auto-loading
            CreateTestSelectionUI();
        }

        void Update()
        {
            if (!GONetMain.IsServer)
                return;

            if (!hasInitialized && testScriptAsset != null && script == null)
            {
                InitializeTest();
            }

            // Keep UI visible during test execution with live updates
            if (isExecuting && instructionUI != null && currentStepDescription != null)
            {
                UpdateLiveStatus();
            }
        }

        private void UpdateLiveStatus()
        {
            // This provides a live heartbeat/status indicator during test execution
            // Shows current step and a simple animation
            float pulseTime = Time.time % 2f;
            string pulse = pulseTime < 1f ? "●" : "○";

            // If no specific instruction is showing, display current step
            // (This won't override human-action or wait messages)
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Cleanup logger if test was interrupted
            testLogger?.Dispose();
            testLogger = null;
        }

        public override void OnGONetParticipantStarted()
        {
            base.OnGONetParticipantStarted();

            if (!GONetMain.IsServer)
            {
                GONetLog.Info("[TestExecutor] Running on client - waiting for server commands");
                return;
            }

            // Test executor ready - user must explicitly select and start a test via UI
            GONetLog.Info("[TestExecutor] Test executor ready - waiting for user to select and start a test");
        }

        public override void OnGONetReady(GONetParticipant gonetParticipant)
        {
            base.OnGONetReady(gonetParticipant);

            // Track beacons spawned by clients (server sees them arrive)
            if (GONetMain.IsServer && gonetParticipant.GetComponent<SpawnTestBeacon>() != null)
            {
                ushort ownerAuthority = gonetParticipant.OwnerAuthorityId;

                // Initialize tracking for this authority if needed
                if (!actualSpawnsByAuthority.ContainsKey(ownerAuthority))
                {
                    actualSpawnsByAuthority[ownerAuthority] = new List<uint>();
                }

                // Track this beacon if not already tracked
                if (!actualSpawnsByAuthority[ownerAuthority].Contains(gonetParticipant.GONetId))
                {
                    actualSpawnsByAuthority[ownerAuthority].Add(gonetParticipant.GONetId);
                    GONetLog.Info($"[TestExecutor] Tracked beacon {gonetParticipant.GONetId} from Authority{ownerAuthority}");
                }
            }
        }


        private void InitializeTest()
        {
            if (hasInitialized)
                return;

            hasInitialized = true;

            // Parse script
            script = GONetTestScript.ParseFromString(testScriptAsset.text);
            if (script == null)
            {
                GONetLog.Error("[TestExecutor] Failed to parse test script!");
                return;
            }

            GONetLog.Info($"[TestExecutor] Loaded test: {script.name}");
            GONetLog.Info(script.ToString());

            // Create test logger for this test run
            testLogger = new GONetTestLogger(script.name);
            testLogger.Log($"Test initialized: {script.name}");
            testLogger.Log($"Description: {script.description}");
            testLogger.Log($"Required clients: {script.requireClients}");
            testLogger.Log($"Total steps: {script.steps.Count}");
            testLogger.Log("");

            // Start execution
            StartCoroutine(ExecuteScript());
        }

        private void CreateTestSelectionUI()
        {
            // Create dedicated canvas for test UI
            GameObject canvasObj = new GameObject("TestExecutorCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Render on top of everything

            // Add CanvasScaler for resolution independence
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Make it persist across scenes
            DontDestroyOnLoad(canvasObj);

            // Ensure EventSystem exists
            EnsureEventSystemExists();

            // Create the test selection UI
            GameObject selectionObj = new GameObject("TestSelectionUI");
            selectionObj.transform.SetParent(canvas.transform, false);
            selectionUI = selectionObj.AddComponent<TestSelectionUI>();
            selectionUI.Initialize();
            selectionUI.TestStartRequested += OnTestStartRequested;

            // Create the instruction UI (initially hidden)
            GameObject instructionObj = new GameObject("TestInstructionUI");
            instructionObj.transform.SetParent(canvas.transform, false);
            instructionUI = instructionObj.AddComponent<TestInstructionUI>();
            instructionUI.Initialize();
            instructionUI.gameObject.SetActive(false); // Start hidden
        }

        private void OnTestStartRequested(string testName)
        {
            GONetLog.Info($"[TestExecutor] Test start requested: {testName}");

            // Load the requested test
            TextAsset testAsset = Resources.Load<TextAsset>($"Tests/{testName}");
            if (testAsset == null)
            {
                GONetLog.Error($"[TestExecutor] Failed to load test: {testName}");
                return;
            }

            testScriptAsset = testAsset;
            InitializeTest();

            // Show instruction UI, hide selection UI
            instructionUI.gameObject.SetActive(true);
            selectionUI.Hide();
        }

        private void CreateInstructionUI()
        {
            // This method is no longer used - UI created in CreateTestSelectionUI
            // Kept for backwards compatibility but not called
        }

        private void EnsureEventSystemExists()
        {
            // Find all EventSystems (both in scene and DontDestroyOnLoad)
            UnityEngine.EventSystems.EventSystem[] allEventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();

            if (allEventSystems.Length == 0)
            {
                // No EventSystem at all - create a persistent one
                GONetLog.Debug("[TestExecutor] No EventSystem found - creating persistent one");
                GameObject eventSystemGO = new GameObject("EventSystem_Persistent");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                DontDestroyOnLoad(eventSystemGO);
                return;
            }

            // Check if we have a persistent EventSystem
            UnityEngine.EventSystems.EventSystem persistentEventSystem = null;
            List<UnityEngine.EventSystems.EventSystem> sceneEventSystems = new List<UnityEngine.EventSystems.EventSystem>();

            foreach (var es in allEventSystems)
            {
                // Objects in DontDestroyOnLoad have scene.name == "DontDestroyOnLoad"
                if (es.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    persistentEventSystem = es;
                }
                else
                {
                    sceneEventSystems.Add(es);
                }
            }

            // If we have a persistent one, destroy ALL scene-based ones
            if (persistentEventSystem != null)
            {
                foreach (var sceneES in sceneEventSystems)
                {
                    GONetLog.Debug($"[TestExecutor] Found scene-based EventSystem '{sceneES.gameObject.name}' while persistent exists - destroying it");
                    Destroy(sceneES.gameObject);
                }
                return;
            }

            // No persistent EventSystem - make the first scene-based one persistent, destroy the rest
            if (sceneEventSystems.Count > 0)
            {
                UnityEngine.EventSystems.EventSystem firstSceneES = sceneEventSystems[0];
                GONetLog.Debug($"[TestExecutor] Making scene-based EventSystem '{firstSceneES.gameObject.name}' persistent");
                DontDestroyOnLoad(firstSceneES.gameObject);

                // Destroy any other scene-based EventSystems
                for (int i = 1; i < sceneEventSystems.Count; i++)
                {
                    GONetLog.Debug($"[TestExecutor] Destroying duplicate scene-based EventSystem '{sceneEventSystems[i].gameObject.name}'");
                    Destroy(sceneEventSystems[i].gameObject);
                }
            }
        }

        private IEnumerator ExecuteScript()
        {
            isExecuting = true;

            GONetLog.Info($"[TestExecutor] ========================================");
            GONetLog.Info($"[TestExecutor] STARTING TEST: {script.name}");
            if (!string.IsNullOrEmpty(script.description))
                GONetLog.Info($"[TestExecutor] {script.description}");
            GONetLog.Info($"[TestExecutor] ========================================");

            for (int i = 0; i < script.steps.Count; i++)
            {
                currentStepIndex = i;
                var step = script.steps[i];
                currentStepDescription = $"Step {i + 1}/{script.steps.Count}: {step.type}";

                GONetLog.Info($"[TestExecutor] {currentStepDescription}");

                yield return ExecuteStep(step);
            }

            // Complete
            isExecuting = false;
            PrintTestResults();
        }

        private IEnumerator ExecuteStep(GONetTestScript.TestStep step)
        {
            // Log step start
            testLogger?.LogStep(currentStepIndex, script.steps.Count, step.type.ToString());

            switch (step.type)
            {
                case GONetTestScript.TestStepType.WaitClients:
                    yield return Step_WaitClients(step.GetParamInt("count", script.requireClients));
                    break;

                case GONetTestScript.TestStepType.SpawnServer:
                    yield return Step_SpawnServer(step.GetParamInt("count", 1));
                    break;

                case GONetTestScript.TestStepType.SpawnClient:
                    yield return Step_SpawnClient(
                        step.GetParamInt("client", 1),
                        step.GetParamInt("count", 1)
                    );
                    break;

                case GONetTestScript.TestStepType.SpawnAllClients:
                    yield return Step_SpawnAllClients(step.GetParamInt("count", 1));
                    break;

                case GONetTestScript.TestStepType.Wait:
                    yield return Step_Wait(step.GetParamFloat("seconds", 1f));
                    break;

                case GONetTestScript.TestStepType.VerifyBeacons:
                    yield return Step_VerifyBeacons(step.GetParam("beacons", "all"));
                    break;

                case GONetTestScript.TestStepType.VerifyDespawned:
                    yield return Step_VerifyDespawned(step.GetParam("beacons", "all"));
                    break;

                case GONetTestScript.TestStepType.VerifyCount:
                    yield return Step_VerifyCount(step.GetParamInt("expected", 0));
                    break;

                case GONetTestScript.TestStepType.SceneChange:
                    yield return Step_SceneChange(step.GetParam("scene"));
                    break;

                case GONetTestScript.TestStepType.WaitDespawn:
                    yield return Step_WaitDespawn(step.GetParamFloat("seconds", script.despawnWaitTime));
                    break;

                case GONetTestScript.TestStepType.HumanAction:
                    yield return Step_HumanAction(step.GetParam("instruction"));
                    break;

                case GONetTestScript.TestStepType.WaitClient:
                    yield return Step_WaitClient(step.GetParamInt("client"));
                    break;

                case GONetTestScript.TestStepType.Log:
                    Step_Log(step.GetParam("message"));
                    break;
            }

            // Update UI after each step to show progress (except for steps that manage their own UI)
            if (step.type != GONetTestScript.TestStepType.WaitClients &&
                step.type != GONetTestScript.TestStepType.HumanAction &&
                step.type != GONetTestScript.TestStepType.WaitClient &&
                step.type != GONetTestScript.TestStepType.WaitDespawn)
            {
                UpdateTestProgressUI();
            }
        }

        // ==================== STEP IMPLEMENTATIONS ====================

        private IEnumerator Step_WaitClients(int count)
        {
            UpdateConnectedClients();

            float elapsedTime = 0f;
            while (connectedClients.Count < count)
            {
                UpdateConnectedClients();
                int remaining = count - connectedClients.Count;

                // Live update with animated dots and elapsed time
                string dots = new string('.', ((int)(elapsedTime * 2f) % 4));
                instructionUI?.SetInstruction(
                    $"🚨 WAITING FOR CLIENTS 🚨\n\n" +
                    $"Required: {count}\n" +
                    $"Connected: {connectedClients.Count}\n\n" +
                    $"Please start {remaining} more client(s) now{dots}\n\n" +
                    $"Waiting: {elapsedTime:F0}s",
                    true);

                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }

            instructionUI?.SetInstruction("✓ All clients connected!\n\nTest continuing...", false);
            yield return new WaitForSeconds(1.5f);
            UpdateTestProgressUI();
            GONetLog.Info($"[TestExecutor] ✓ All {count} clients connected");
        }

        private IEnumerator Step_SpawnServer(int count)
        {
            GONetLog.Info($"[TestExecutor] Spawning {count} beacons from SERVER...");

            ushort serverAuthority = GONetMain.OwnerAuthorityId_Server;

            // Track expected spawns
            if (!expectedSpawnsByAuthority.ContainsKey(serverAuthority))
            {
                expectedSpawnsByAuthority[serverAuthority] = 0;
                actualSpawnsByAuthority[serverAuthority] = new List<uint>();
            }
            expectedSpawnsByAuthority[serverAuthority] += count;

            for (int i = 0; i < count; i++)
            {
                var beacon = SpawnBeacon();
                if (beacon != null)
                {
                    // Wait for GONetId to be assigned (happens asynchronously after instantiation)
                    yield return new WaitUntil(() => beacon.GONetParticipant.GONetId > 0);

                    trackedBeaconIds.Add(beacon.GONetParticipant.GONetId);
                    actualSpawnsByAuthority[serverAuthority].Add(beacon.GONetParticipant.GONetId);
                    GONetLog.Info($"[TestExecutor] SERVER spawned beacon {beacon.GONetParticipant.GONetId}");
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator Step_SpawnClient(int clientId, int count)
        {
            GONetLog.Info($"[TestExecutor] Commanding Client{clientId} to spawn {count} beacons...");

            ushort authorityId = (ushort)clientId;

            // Track expected spawns from this client
            if (!expectedSpawnsByAuthority.ContainsKey(authorityId))
            {
                expectedSpawnsByAuthority[authorityId] = 0;
                actualSpawnsByAuthority[authorityId] = new List<uint>();
            }
            expectedSpawnsByAuthority[authorityId] += count;

            RPC_CommandClientToSpawn(count, authorityId);

            yield return new WaitForSeconds(count * 0.3f + 1f); // Wait for spawns
        }

        private IEnumerator Step_SpawnAllClients(int count)
        {
            GONetLog.Info($"[TestExecutor] Commanding ALL clients to spawn {count} beacons each...");

            foreach (var clientId in connectedClients)
            {
                RPC_CommandClientToSpawn(count, clientId);
            }

            yield return new WaitForSeconds(count * 0.3f * connectedClients.Count + 1f);
        }

        private IEnumerator Step_Wait(float seconds)
        {
            GONetLog.Info($"[TestExecutor] Waiting {seconds}s...");
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator Step_VerifyBeacons(string beaconSpec)
        {
            GONetLog.Info($"[TestExecutor] Verifying beacons: {beaconSpec}");

            List<uint> beaconsToCheck = beaconSpec.ToLower() == "all"
                ? trackedBeaconIds
                : beaconSpec.Split(',').Select(s => uint.Parse(s.Trim())).ToList();

            bool allExist = true;
            int missingCount = 0;

            foreach (var id in beaconsToCheck)
            {
                var gnp = GONetMain.GetGONetParticipantById(id);
                if (gnp == null)
                {
                    allExist = false;
                    missingCount++;
                    GONetLog.Warning($"[TestExecutor] ❌ Beacon {id} NOT FOUND on server!");
                }
            }

            string result = allExist
                ? $"✓ All {beaconsToCheck.Count} beacons exist"
                : $"❌ {missingCount}/{beaconsToCheck.Count} beacons MISSING";

            RecordTestResult($"Verify Beacons ({beaconSpec})", allExist, result);

            yield return null;
        }

        private IEnumerator Step_VerifyDespawned(string beaconSpec)
        {
            GONetLog.Info($"[TestExecutor] Verifying beacons despawned: {beaconSpec}");

            List<uint> beaconsToCheck = beaconSpec.ToLower() == "all"
                ? trackedBeaconIds
                : beaconSpec.Split(',').Select(s => uint.Parse(s.Trim())).ToList();

            bool allDespawned = true;
            int stillExistCount = 0;

            foreach (var id in beaconsToCheck)
            {
                var gnp = GONetMain.GetGONetParticipantById(id);
                if (gnp != null)
                {
                    allDespawned = false;
                    stillExistCount++;
                    GONetLog.Warning($"[TestExecutor] ❌ Beacon {id} STILL EXISTS!");
                }
            }

            string result = allDespawned
                ? $"✓ All {beaconsToCheck.Count} beacons despawned"
                : $"❌ {stillExistCount}/{beaconsToCheck.Count} beacons STILL EXIST";

            RecordTestResult($"Verify Despawned ({beaconSpec})", allDespawned, result);

            yield return null;
        }

        private IEnumerator Step_VerifyCount(int expectedCount)
        {
            int actualCount = GONetMain.gonetParticipantByGONetIdMap.Values
                .Count(p => p.GetComponent<SpawnTestBeacon>() != null);

            bool matches = actualCount == expectedCount;
            string result = matches
                ? $"✓ Beacon count matches: {actualCount}"
                : $"❌ Expected {expectedCount} beacons, found {actualCount}";

            RecordTestResult("Verify Beacon Count", matches, result);

            yield return null;
        }

        private IEnumerator Step_SceneChange(string sceneName)
        {
            GONetLog.Info($"[TestExecutor] Changing scene to: {sceneName}");

            // Clear tracked beacons (scene change despawns them)
            var oldBeacons = new List<uint>(trackedBeaconIds);
            trackedBeaconIds.Clear();

            GONetMain.SceneManager.LoadSceneFromBuildSettings(sceneName, LoadSceneMode.Single);

            yield return new WaitForSeconds(3f); // Wait for scene load

            // Verify old beacons were despawned
            int remainingCount = 0;
            foreach (var id in oldBeacons)
            {
                var gnp = GONetMain.GetGONetParticipantById(id);
                if (gnp != null)
                    remainingCount++;
            }

            bool cleanedUp = remainingCount == 0;
            string result = cleanedUp
                ? $"✓ Scene changed, {oldBeacons.Count} beacons cleaned up"
                : $"❌ Scene changed, but {remainingCount} beacons still exist!";

            RecordTestResult($"Scene Change to {sceneName}", cleanedUp, result);
        }

        private IEnumerator Step_WaitDespawn(float seconds)
        {
            GONetLog.Info($"[TestExecutor] Waiting {seconds}s for natural despawn...");

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;

                float remaining = seconds - elapsed;
                instructionUI?.SetInstruction($"⏱ WAITING FOR DESPAWN ⏱\n\nTime remaining: {remaining:F0}s\n\nBeacons should despawn naturally...", false);
            }

            UpdateTestProgressUI();
        }

        private IEnumerator Step_HumanAction(string instruction)
        {
            GONetLog.Info($"[TestExecutor] HUMAN ACTION REQUIRED: {instruction}");

            instructionUI?.SetInstruction($"🚨 HUMAN ACTION REQUIRED 🚨\n\n{instruction}\n\nPress SPACE when complete", true);

            // Wait for human to press space
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }

            instructionUI?.SetInstruction("✓ Action completed!\n\nTest continuing...", false);
            yield return new WaitForSeconds(1f);
            UpdateTestProgressUI();
            GONetLog.Info($"[TestExecutor] Human action completed");
        }

        private IEnumerator Step_WaitClient(int clientId)
        {
            ushort authorityId = (ushort)clientId;

            GONetLog.Info($"[TestExecutor] Waiting for Client{clientId} (Authority{authorityId}) to connect...");

            instructionUI?.SetInstruction($"⏳ WAITING FOR CLIENT {clientId} ⏳\n\nPlease start Client{clientId} now", true);

            while (!connectedClients.Contains(authorityId))
            {
                UpdateConnectedClients();
                yield return new WaitForSeconds(0.5f);
            }

            instructionUI?.SetInstruction($"✓ Client{clientId} connected!\n\nSynchronizing...", false);
            yield return new WaitForSeconds(2f);
            UpdateTestProgressUI();
            GONetLog.Info($"[TestExecutor] ✓ Client{clientId} connected");
        }

        private void Step_Log(string message)
        {
            GONetLog.Info($"[TestExecutor] {message}");
        }

        // ==================== HELPERS ====================

        private void UpdateTestProgressUI()
        {
            if (instructionUI == null)
                return;

            string progressText = $"📋 TEST IN PROGRESS 📋\n\n" +
                                  $"Test: {script.name}\n" +
                                  $"Step: {currentStepIndex + 1}/{script.steps.Count}\n" +
                                  $"Passed: {testResults.Count(r => r.passed)} | Failed: {testResults.Count(r => !r.passed)}";

            instructionUI.SetInstruction(progressText, false);
        }

        private void UpdateConnectedClients()
        {
            connectedClients.Clear();
            foreach (var gnp in GONetMain.gonetParticipantByGONetIdMap.Values)
            {
                if (gnp.OwnerAuthorityId != GONetMain.OwnerAuthorityId_Server &&
                    gnp.OwnerAuthorityId != GONetMain.MyAuthorityId)
                {
                    connectedClients.Add(gnp.OwnerAuthorityId);
                }
            }
        }

        private SpawnTestBeacon SpawnBeacon()
        {
            var prefab = Resources.Load<GameObject>("SpawnTestBeacon");
            if (prefab == null)
            {
                GONetLog.Error("[TestExecutor] Failed to load SpawnTestBeacon prefab!");
                return null;
            }

            // Spawn beacons in a more visible area
            // For GONetSample scene: spread around origin at eye level
            // For ProjectileTest scene: similar visible area
            Vector3 randomPos = new Vector3(
                UnityEngine.Random.Range(-8f, 8f),   // X: wider spread
                UnityEngine.Random.Range(1f, 3f),    // Y: eye level (1-3m height)
                UnityEngine.Random.Range(3f, 10f)    // Z: forward from camera
            );

            var instance = Instantiate(prefab, randomPos, Quaternion.identity);
            GONetLog.Debug($"[TestExecutor] Spawned beacon at position {randomPos}");
            return instance.GetComponent<SpawnTestBeacon>();
        }

        [ClientRpc]
        internal void RPC_CommandClientToSpawn(int count, ushort targetClientId)
        {
            if (!GONetMain.IsClient || GONetMain.MyAuthorityId != targetClientId)
                return;

            GONetLog.Info($"[TestExecutor] CLIENT: Received spawn command for {count} beacons");
            StartCoroutine(SpawnBeaconsOnClient(count));
        }

        private IEnumerator SpawnBeaconsOnClient(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var beacon = SpawnBeacon();
                if (beacon != null)
                {
                    // Wait for GONetId to be assigned (happens asynchronously after instantiation)
                    yield return new WaitUntil(() => beacon.GONetParticipant.GONetId > 0);

                    GONetLog.Info($"[TestExecutor] CLIENT spawned beacon {beacon.GONetParticipant.GONetId}");
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void RecordTestResult(string testName, bool passed, string details)
        {
            testResults.Add(new TestResult
            {
                testName = testName,
                passed = passed,
                details = details,
                timestamp = DateTime.Now
            });

            string status = passed ? "✓ PASSED" : "❌ FAILED";
            GONetLog.Info($"[TestExecutor] {status}: {testName}");
            if (!string.IsNullOrEmpty(details))
                GONetLog.Info($"[TestExecutor]   {details}");

            // Also log to test logger
            if (passed)
            {
                testLogger?.LogPass(testName);
            }
            else
            {
                testLogger?.LogFail(testName, details);
            }
        }

        private void PrintTestResults()
        {
            GONetLog.Info("[TestExecutor] ========================================");
            GONetLog.Info($"[TestExecutor] TEST COMPLETE: {script.name}");
            GONetLog.Info("[TestExecutor] ========================================");

            int passed = testResults.Count(r => r.passed);
            int failed = testResults.Count - passed;

            foreach (var result in testResults)
            {
                string status = result.passed ? "✓ PASS" : "❌ FAIL";
                GONetLog.Info($"[TestExecutor] {status} | {result.testName}");
                if (!string.IsNullOrEmpty(result.details))
                    GONetLog.Info($"[TestExecutor]        {result.details}");
            }

            GONetLog.Info("[TestExecutor] ========================================");
            GONetLog.Info($"[TestExecutor] TOTAL: {passed} passed, {failed} failed");
            GONetLog.Info("[TestExecutor] ========================================");

            // Write summary to test log
            testLogger?.LogSummary(passed, failed);
            string logPath = testLogger?.GetLogFilePath();
            testLogger?.Dispose();
            testLogger = null;

            string finalMessage = $"✅ TEST COMPLETE ✅\n\n{script.name}\n\n✓ Passed: {passed}\n✗ Failed: {failed}\n\nLog saved to:\n{logPath}";
            bool hadFailures = failed > 0;
            instructionUI?.SetInstruction(finalMessage, hadFailures); // Red background if failures
        }
    }

    [System.Serializable]
    internal class TestResult
    {
        public string testName;
        public bool passed;
        public string details;
        public DateTime timestamp;
    }
}
