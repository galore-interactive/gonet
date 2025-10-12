================================================================================
GONet Log Analysis Scripts - README
================================================================================

PURPOSE:
    Comprehensive log analysis tools for investigating OnGONetReady reliability,
    frame timing, and complete lifecycle tracking across ALL GONet participants
    (beacons, projectiles, physics cubes, etc.).

    Tracks lifecycle from earliest point (Awake) through OnGONetReady using
    Unity InstanceID correlation for complete participant stories.

================================================================================
SCRIPT: analyze_ongonetready_timing.py
================================================================================

WHAT IT DOES:
    - Parses GONet log files to extract OnGONetReady timing metrics
    - Analyzes frame delays between Start() and OnGONetReady callbacks
    - Generates statistics by peer (Server vs Client)
    - Lists participants that NEVER fired OnGONetReady
    - Provides breakdown by GameObject type

REQUIREMENTS:
    1. Python 3.x installed
    2. GONetGlobal.OnGONetReady() logging enabled (see SETUP below)
    3. SpawnTestBeacon or custom scripts logging "Start() called"

SETUP - Enable Universal Lifecycle Logging:
    1. Open: Assets/GONet/Code/GONet/Core/GONetParticipant.cs
    2. Find: Awake() method (around line 818)
    3. Verify EARLIEST lifecycle logging is ENABLED (should be enabled by default):

       GONetLog.Info($"[GONetParticipant] 🔵 Awake() START - InstanceID: {GetInstanceID()}, GameObject: {gameObject.name}");

    4. Open: Assets/GONet/Code/GONet/Main/GONetGlobal.cs
    5. Find: OnGONetReady() method (around line 950)
    6. Verify OnGONetReady logging is ENABLED (should be enabled by default):

       GONetLog.Info($"[GONetGlobal] ✅ OnGONetReady FIRED - InstanceID: {gonetParticipant.GetInstanceID()}, GONetId: {gonetParticipant.GONetId}, GameObject: {gonetParticipant.name}, IsMine: {gonetParticipant.IsMine}, Owner: {gonetParticipant.OwnerAuthorityId}");

    7. BOTH logging points capture ALL GONet participants regardless of type!

LIFECYCLE CORRELATION WITH INSTANCEID:
    - Unity's InstanceID is available immediately in Awake() (GONetId is NOT yet available)
    - InstanceID persists throughout the object's lifetime
    - Use InstanceID to correlate Awake → OnGONetReady events
    - GONetId becomes available after Awake completes, logged in OnGONetReady
    - This enables tracking the complete lifecycle story from earliest point to ready state

USAGE:
    # Default (uses hardcoded log path)
    python3 analyze_ongonetready_timing.py

    # Custom log file
    python3 analyze_ongonetready_timing.py /path/to/your/logfile.log

    # Windows (WSL)
    python3 analyze_ongonetready_timing.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-10-11.log"

OUTPUT SECTIONS:
    1. SUMMARY BY PEER
       - Total participants count
       - OnGONetReady fired count and percentage
       - OnGONetReady NEVER fired count
       - Average/min/max frame delays
       - Frame delay distribution histogram

    2. PARTICIPANTS THAT NEVER FIRED OnGONetReady
       - Lists each participant with GONetId, GameObject name, and StartFrame
       - Critical for identifying lifecycle bugs

    3. BREAKDOWN BY GAMEOBJECT TYPE
       - Per-type statistics (SpawnTestBeacon, CannonBall, PhysicsCube, etc.)
       - Fired/never fired counts
       - Average/min/max frame delays per type

    4. DETAILED RESULTS SAMPLE
       - First 20 participants with full timing details
       - Useful for spot-checking specific cases

INTERPRETING RESULTS:
    ✅ SUCCESS: 100% OnGONetReady fired, frame delays consistent (1-3 frames typical)
    ⚠️ WARNING: <100% OnGONetReady fired indicates lifecycle issues
    ❌ CRITICAL: Large negative frame delays or "NEVER" fired indicates race conditions

EXAMPLE OUTPUT:
    ============================================================
    SUMMARY BY PEER
    ============================================================

    --- Server ---
    Total participants: 141
    [OK] OnGONetReady fired: 133 (94.3%)
    [!!] OnGONetReady NEVER fired: 8

    Average frame delay: 1.52 frames
    Min frame delay: 1 frame(s)
    Max frame delay: 3 frame(s)

    Frame delay distribution:
       1 frames:  105 participants ( 78.9%)
       2 frames:   25 participants ( 18.8%)
       3 frames:    3 participants (  2.3%)

    --- Client:1 ---
    Total participants: 143
    [OK] OnGONetReady fired: 140 (97.9%)
    [!!] OnGONetReady NEVER fired: 3
    ...

================================================================================
SCRIPT: analyze_ongonetready_timing.sh
================================================================================

WHAT IT DOES:
    - Bash version of the Python script (same functionality)
    - Uses grep/sed/awk for parsing (no Python required)

REQUIREMENTS:
    - Bash shell (Linux, macOS, WSL on Windows)
    - Standard Unix tools: grep, sed, awk, sort, uniq

USAGE:
    # Default (uses hardcoded log path)
    bash analyze_ongonetready_timing.sh

    # Custom log file
    bash analyze_ongonetready_timing.sh /path/to/your/logfile.log

NOTES:
    - Temporary files created in /tmp (cleaned up on exit)
    - Full results can be piped to 'less' for paging

================================================================================
TROUBLESHOOTING
================================================================================

ISSUE: "No Start() events extracted"
FIX:   - Verify your custom scripts log "Start() called" with GONetId
       - Pattern: GONetLog.Info($"... Start() called ... GONetId: {GONetParticipant.GONetId} ...")

ISSUE: "Only SpawnTestBeacon showing in breakdown"
FIX:   - Enable GONetGlobal.OnGONetReady() logging (see SETUP above)
       - This captures ALL participant types automatically

ISSUE: "Script not finding log file"
FIX:   - Check default log path in script (line 205-206 in Python, line 5 in Bash)
       - Update path to match your Unity project name and log location
       - Windows: Use forward slashes in path (C:/Users/... not C:\Users\...)

ISSUE: "Permission denied when running script"
FIX:   - Make script executable: chmod +x analyze_ongonetready_timing.sh
       - Or run via interpreter: bash analyze_ongonetready_timing.sh

================================================================================
CUSTOMIZATION
================================================================================

TO ANALYZE DIFFERENT LOG PATTERNS:
    1. Modify parse_log() function in Python script (lines 13-49)
    2. Update regex patterns to match your custom log format
    3. Ensure you extract: peer, gonetid, frame, gameobject

TO ADD NEW OUTPUT SECTIONS:
    1. Add new calculation function (e.g., calculate_despawn_stats())
    2. Call from main() function (after line 225)
    3. Format output similar to existing sections

TO EXPORT RESULTS TO CSV:
    1. Modify main() to write CSV instead of printing
    2. Use Python's csv module or bash redirect > output.csv

================================================================================
VERSION HISTORY
================================================================================

2025-10-11: Initial version
    - Created Python and Bash variants
    - Support for peer-based analysis (Server vs Client)
    - GameObject type breakdown
    - Frame timing histograms
    - Universal GONetGlobal.OnGONetReady() logging enabled
    - Universal GONetParticipant.Awake() logging enabled
    - InstanceID-based lifecycle correlation (Awake → OnGONetReady)

================================================================================
CONTACT
================================================================================

These scripts are part of the GONet networking framework.
For issues or questions, contact: contactus@galoreinteractive.com

================================================================================
