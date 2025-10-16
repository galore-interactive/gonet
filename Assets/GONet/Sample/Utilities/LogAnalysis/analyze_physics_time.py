#!/usr/bin/env python3
"""
Analyzes GONet physics time logs to detect ping-pong behavior and verify monotonicity.

Usage:
    python analyze_physics_time.py <log_file_path>

Example:
    python analyze_physics_time.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-10-16.log"
"""

import sys
import re
from dataclasses import dataclass
from typing import List, Optional, Tuple
from enum import Enum


class UpdateType(Enum):
    UPDATE = "Update"
    FIXED_UPDATE = "FixedUpdate"


@dataclass
class TimeEntry:
    """Single time measurement from logs"""
    line_number: int
    timestamp: str
    update_type: UpdateType
    gonet_fixed: float
    gonet_std: float
    unity_fixed: float
    unity_std: float
    unity_realtime: float
    debug_stopwatch: float

    # Optional catchup info
    catchup_iterations: Optional[int] = None
    catchup_from: Optional[float] = None
    catchup_to: Optional[float] = None
    catchup_target: Optional[float] = None


@dataclass
class AnalysisResult:
    """Results of time analysis"""
    total_entries: int
    update_entries: int
    fixed_update_entries: int

    # Monotonicity checks
    gonet_fixed_violations: List[Tuple[int, float, float]]  # (line, prev, curr)
    gonet_std_violations: List[Tuple[int, float, float]]

    # Ping-pong detection (fixed < std when it shouldn't be)
    ping_pong_violations: List[Tuple[int, float, float]]  # (line, fixed, std)

    # Gap analysis
    max_gonet_fixed_gap: float
    max_gonet_std_gap: float
    avg_gonet_fixed_delta: float
    avg_gonet_std_delta: float

    # Catchup statistics
    total_catchups: int
    total_catchup_iterations: int
    max_catchup_iterations: int
    catchup_lines: List[int]

    # Overall health
    is_monotonic: bool
    has_ping_pong: bool
    overall_health: str  # "GOOD", "WARNING", "FAILED"


def detect_session_reset(line: str) -> bool:
    """
    Detect if this line indicates a session reset (new client, scene reload, etc.)

    Indicators:
    - "Initialized - Anchored to network time"
    - Frame count goes backward
    - Time significantly smaller than previous
    """
    return "Initialized - Anchored to network time" in line


def is_server_log_line(line: str) -> bool:
    """Check if log line is from server"""
    return '[Server]' in line or '[DEBUG][Server]' in line or '[INFO][Server]' in line

def is_client_log_line(line: str) -> bool:
    """Check if log line is from client"""
    return '[Client' in line or '[DEBUG][Client' in line or '[INFO][Client' in line

def parse_physics_time_line(line: str, line_number: int) -> Optional[TimeEntry]:
    """
    Parse a [PhysicsTime] log line.

    Expected formats:
    [PhysicsTime] Update, gonet.std:0.1920540  unity.std:0.0010014  unity.realtimeSinceStartup:0.0010014
    [PhysicsTime] FixedUpdate, gonet.fixed:0.4846504  gonet.std:0.4846504  unity.fixed:0.0200000  unity.std:0.0200000  unity.realtimeSinceStartup:0.4846504
    """
    # Check if this is a PhysicsTime log line
    if '[PhysicsTime]' not in line:
        return None

    # Extract update type (handles both "Update," and "Update[hashcode],")
    if 'FixedUpdate' in line and ('FixedUpdate,' in line or 'FixedUpdate[' in line):
        update_type = UpdateType.FIXED_UPDATE
    elif 'Update' in line and ('Update,' in line or 'Update[' in line) and 'FixedUpdate' not in line:
        update_type = UpdateType.UPDATE
    else:
        return None

    # Extract timestamp (if present)
    timestamp_match = re.search(r'(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3})', line)
    timestamp = timestamp_match.group(1) if timestamp_match else ""

    # Extract time values
    def extract_float(pattern: str) -> float:
        match = re.search(pattern + r':([-\d.]+)', line)
        if match:
            value_str = match.group(1).rstrip('.')  # Remove trailing period if present
            try:
                return float(value_str)
            except ValueError:
                return 0.0
        return 0.0

    gonet_fixed = extract_float(r'gonet\.fixed')
    gonet_std = extract_float(r'gonet\.std')
    unity_fixed = extract_float(r'unity\.fixed')
    unity_std = extract_float(r'unity\.std')
    unity_realtime = extract_float(r'unity\.realtimeSinceStartup')
    debug_stopwatch = extract_float(r'debugStopwatch')

    return TimeEntry(
        line_number=line_number,
        timestamp=timestamp,
        update_type=update_type,
        gonet_fixed=gonet_fixed,
        gonet_std=gonet_std,
        unity_fixed=unity_fixed,
        unity_std=unity_std,
        unity_realtime=unity_realtime,
        debug_stopwatch=debug_stopwatch
    )


def parse_catchup_line(line: str, line_number: int) -> Optional[Tuple[int, float, float, float]]:
    """
    Parse a catchup log line.

    Expected format:
    [PhysicsTime] Caught up 17 physics steps (from 0.200000s to 0.500300s, target: 0.500000s)

    Returns: (iterations, from_time, to_time, target_time)
    """
    if 'Caught up' not in line:
        return None

    match = re.search(r'Caught up (\d+) physics steps \(from ([\d.]+)s to ([\d.]+)s, target: ([\d.]+)s\)', line)
    if match:
        return (
            int(match.group(1)),    # iterations
            float(match.group(2)),  # from
            float(match.group(3)),  # to
            float(match.group(4))   # target
        )
    return None


def analyze_time_entries(entries: List[TimeEntry]) -> AnalysisResult:
    """Analyze parsed time entries for issues"""

    gonet_fixed_violations = []
    gonet_std_violations = []
    ping_pong_violations = []

    gonet_fixed_deltas = []
    gonet_std_deltas = []

    prev_gonet_fixed = None
    prev_gonet_std = None

    for entry in entries:
        # Detect large backward jumps as session resets (> 1 second backward)
        if prev_gonet_fixed is not None and entry.gonet_fixed > 0:
            if entry.gonet_fixed < prev_gonet_fixed - 1.0:  # 1 second threshold
                # This is a session reset, not a violation - reset baseline
                prev_gonet_fixed = None
                prev_gonet_std = None

        if prev_gonet_std is not None and entry.gonet_std > 0:
            if entry.gonet_std < prev_gonet_std - 1.0:  # 1 second threshold
                # This is a session reset, not a violation - reset baseline
                prev_gonet_std = None

        # Check monotonicity for gonet.fixed
        if entry.gonet_fixed > 0:
            if prev_gonet_fixed is not None and entry.gonet_fixed < prev_gonet_fixed:
                gonet_fixed_violations.append((entry.line_number, prev_gonet_fixed, entry.gonet_fixed))
            elif prev_gonet_fixed is not None:
                gonet_fixed_deltas.append(entry.gonet_fixed - prev_gonet_fixed)
            prev_gonet_fixed = entry.gonet_fixed

        # Check monotonicity for gonet.std
        if entry.gonet_std > 0:
            if prev_gonet_std is not None and entry.gonet_std < prev_gonet_std:
                gonet_std_violations.append((entry.line_number, prev_gonet_std, entry.gonet_std))
            elif prev_gonet_std is not None:
                gonet_std_deltas.append(entry.gonet_std - prev_gonet_std)
            prev_gonet_std = entry.gonet_std

        # Check for ping-pong (fixed < std with significant gap)
        if entry.gonet_fixed > 0 and entry.gonet_std > 0:
            gap = entry.gonet_std - entry.gonet_fixed
            if gap > 0.005:  # 5ms threshold (allow small caching differences)
                ping_pong_violations.append((entry.line_number, entry.gonet_fixed, entry.gonet_std))

    # Calculate statistics
    max_fixed_gap = max(gonet_fixed_deltas) if gonet_fixed_deltas else 0
    max_std_gap = max(gonet_std_deltas) if gonet_std_deltas else 0
    avg_fixed_delta = sum(gonet_fixed_deltas) / len(gonet_fixed_deltas) if gonet_fixed_deltas else 0
    avg_std_delta = sum(gonet_std_deltas) / len(gonet_std_deltas) if gonet_std_deltas else 0

    # Count entries by type
    update_count = sum(1 for e in entries if e.update_type == UpdateType.UPDATE)
    fixed_update_count = sum(1 for e in entries if e.update_type == UpdateType.FIXED_UPDATE)

    # Determine overall health
    is_monotonic = len(gonet_fixed_violations) == 0 and len(gonet_std_violations) == 0
    has_ping_pong = len(ping_pong_violations) > 0

    if is_monotonic and not has_ping_pong:
        health = "GOOD"
    elif is_monotonic and has_ping_pong:
        health = "WARNING"
    else:
        health = "FAILED"

    return AnalysisResult(
        total_entries=len(entries),
        update_entries=update_count,
        fixed_update_entries=fixed_update_count,
        gonet_fixed_violations=gonet_fixed_violations,
        gonet_std_violations=gonet_std_violations,
        ping_pong_violations=ping_pong_violations,
        max_gonet_fixed_gap=max_fixed_gap,
        max_gonet_std_gap=max_std_gap,
        avg_gonet_fixed_delta=avg_fixed_delta,
        avg_gonet_std_delta=avg_std_delta,
        total_catchups=0,  # Will be filled by parse_log
        total_catchup_iterations=0,
        max_catchup_iterations=0,
        catchup_lines=[],
        is_monotonic=is_monotonic,
        has_ping_pong=has_ping_pong,
        overall_health=health
    )


def parse_log_file(file_path: str, server_only: bool = False) -> Tuple[List[TimeEntry], AnalysisResult]:
    """Parse log file and return entries + analysis

    Args:
        file_path: Path to log file
        server_only: If True, only parse server logs (ignores client logs)
    """

    entries = []
    catchup_iterations_total = 0
    catchup_count = 0
    max_catchup = 0
    catchup_lines = []

    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            for line_number, line in enumerate(f, 1):
                # Filter by server/client if requested
                if server_only and not is_server_log_line(line):
                    continue

                # Parse time entry
                entry = parse_physics_time_line(line, line_number)
                if entry:
                    entries.append(entry)

                # Parse catchup info
                catchup_info = parse_catchup_line(line, line_number)
                if catchup_info:
                    iterations, from_time, to_time, target = catchup_info
                    catchup_count += 1
                    catchup_iterations_total += iterations
                    max_catchup = max(max_catchup, iterations)
                    catchup_lines.append(line_number)

                    # Attach catchup info to last FixedUpdate entry
                    if entries and entries[-1].update_type == UpdateType.FIXED_UPDATE:
                        entries[-1].catchup_iterations = iterations
                        entries[-1].catchup_from = from_time
                        entries[-1].catchup_to = to_time
                        entries[-1].catchup_target = target

    except FileNotFoundError:
        print(f"ERROR: Log file not found: {file_path}")
        sys.exit(1)
    except Exception as e:
        print(f"ERROR: Failed to read log file: {e}")
        sys.exit(1)

    # Analyze entries
    result = analyze_time_entries(entries)

    # Add catchup stats
    result.total_catchups = catchup_count
    result.total_catchup_iterations = catchup_iterations_total
    result.max_catchup_iterations = max_catchup
    result.catchup_lines = catchup_lines

    return entries, result


def print_report(result: AnalysisResult, entries: List[TimeEntry]):
    """Print analysis report"""

    print("=" * 80)
    print("GONet Physics Time Analysis Report")
    print("=" * 80)
    print()

    # Overall health
    health_color = {
        "GOOD": "[OK]",
        "WARNING": "[WARN]",
        "FAILED": "[FAIL]"
    }
    print(f"Overall Health: {health_color.get(result.overall_health, '?')} {result.overall_health}")
    print()

    # Entry counts
    print("=" * 80)
    print("Entry Statistics")
    print("=" * 80)
    print(f"Total entries:       {result.total_entries}")
    print(f"Update() calls:      {result.update_entries}")
    print(f"FixedUpdate() calls: {result.fixed_update_entries}")
    print()

    # Monotonicity
    print("=" * 80)
    print("Monotonicity Check")
    print("=" * 80)
    if result.is_monotonic:
        print("[OK] All time values progress monotonically (no backward jumps)")
    else:
        print("[FAIL] FAILED: Time values jumped backward!")

        if result.gonet_fixed_violations:
            print(f"\ngonet.fixed violations ({len(result.gonet_fixed_violations)}):")
            for line, prev, curr in result.gonet_fixed_violations[:10]:
                print(f"  Line {line}: {prev:.7f}s -> {curr:.7f}s (BACKWARD!)")

        if result.gonet_std_violations:
            print(f"\ngonet.std violations ({len(result.gonet_std_violations)}):")
            for line, prev, curr in result.gonet_std_violations[:10]:
                print(f"  Line {line}: {prev:.7f}s -> {curr:.7f}s (BACKWARD!)")
    print()

    # Ping-pong detection
    print("=" * 80)
    print("Ping-Pong Detection (fixed < std)")
    print("=" * 80)
    if not result.has_ping_pong:
        print("[OK] No ping-pong detected (fixed time never lags behind standard time)")
    else:
        print(f"[WARN] WARNING: Found {len(result.ping_pong_violations)} instances where fixed < std")
        print("\nSample violations (first 10):")
        for line, fixed, std in result.ping_pong_violations[:10]:
            gap = std - fixed
            print(f"  Line {line}: fixed={fixed:.7f}s, std={std:.7f}s (gap: {gap*1000:.2f}ms)")
    print()

    # Time progression statistics
    print("=" * 80)
    print("Time Progression Statistics")
    print("=" * 80)
    print(f"gonet.fixed:")
    print(f"  Average delta:  {result.avg_gonet_fixed_delta*1000:.2f}ms")
    print(f"  Max delta:      {result.max_gonet_fixed_gap*1000:.2f}ms")
    print()
    print(f"gonet.std:")
    print(f"  Average delta:  {result.avg_gonet_std_delta*1000:.2f}ms")
    print(f"  Max delta:      {result.max_gonet_std_gap*1000:.2f}ms")
    print()

    # Catchup statistics
    if result.total_catchups > 0:
        print("=" * 80)
        print("Catchup Statistics")
        print("=" * 80)
        print(f"Total catchups:           {result.total_catchups}")
        print(f"Total iterations:         {result.total_catchup_iterations}")
        print(f"Max iterations (single):  {result.max_catchup_iterations}")
        print(f"Average iterations:       {result.total_catchup_iterations / result.total_catchups:.1f}")
        print()

        # Show catchup events
        if result.catchup_lines:
            print("Catchup events (first 5):")
            for line_num in result.catchup_lines[:5]:
                # Find corresponding entry
                matching_entries = [e for e in entries if e.line_number == line_num and e.catchup_iterations]
                if matching_entries:
                    e = matching_entries[0]
                    print(f"  Line {line_num}: {e.catchup_iterations} steps " +
                          f"({e.catchup_from:.6f}s → {e.catchup_to:.6f}s, target: {e.catchup_target:.6f}s)")
        print()

    # Sample time progression
    print("=" * 80)
    print("Sample Time Progression (first 10 entries)")
    print("=" * 80)
    print(f"{'Line':<6} {'Type':<12} {'gonet.fixed':<14} {'gonet.std':<14} {'Gap (ms)':<10}")
    print("-" * 80)
    for entry in entries[:10]:
        gap = (entry.gonet_std - entry.gonet_fixed) * 1000 if entry.gonet_fixed > 0 else 0
        fixed_str = f"{entry.gonet_fixed:.7f}" if entry.gonet_fixed > 0 else "N/A"
        std_str = f"{entry.gonet_std:.7f}" if entry.gonet_std > 0 else "N/A"
        print(f"{entry.line_number:<6} {entry.update_type.value:<12} {fixed_str:<14} {std_str:<14} {gap:>8.2f}")
    print()

    # Final verdict
    print("=" * 80)
    print("Final Verdict")
    print("=" * 80)
    if result.overall_health == "GOOD":
        print("[OK] Implementation is WORKING CORRECTLY")
        print("   - All time values progress monotonically")
        print("   - No ping-pong behavior detected")
        print("   - Fixed time stays synchronized with standard time")
    elif result.overall_health == "WARNING":
        print("[WARN] Implementation has MINOR ISSUES")
        print("   - Time values progress monotonically (GOOD)")
        print("   - But fixed time occasionally lags behind standard time")
        print("   - May need catchup tuning or is within acceptable tolerance")
    else:
        print("[FAIL] Implementation has CRITICAL ISSUES")
        print("   - Time values jumping backward (MONOTONICITY VIOLATED)")
        print("   - This will cause sync problems in production")
        print("   - Requires immediate fix")
    print("=" * 80)


def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_physics_time.py <log_file_path> [--server-only]")
        print("\nExample:")
        print('  python analyze_physics_time.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-10-16.log"')
        print('  python analyze_physics_time.py "gonet-2025-10-16.log" --server-only')
        sys.exit(1)

    log_file = sys.argv[1]
    server_only = '--server-only' in sys.argv

    print(f"Analyzing log file: {log_file}")
    if server_only:
        print("Mode: SERVER ONLY (ignoring client logs)")
    print("Parsing...")

    entries, result = parse_log_file(log_file, server_only=server_only)

    if not entries:
        print("\nWARNING: No [PhysicsTime] entries found in log file!")
        print("Make sure the log contains debug output with [PhysicsTime] tags.")
        sys.exit(1)

    print(f"Found {len(entries)} time entries\n")

    print_report(result, entries)


if __name__ == "__main__":
    main()
