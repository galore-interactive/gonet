#!/usr/bin/env python3
"""
Analyze quantization-aware anchoring performance from GONet log files.

Usage:
    python analyze_quantization_anchoring.py <log_file_path> [--gonetid <id>]

This script analyzes:
1. VELOCITY bundle vs VALUE bundle ratios for position sync
2. Quantization-aware anchor frequency and timing
3. Time-based fallback anchor frequency
4. Quantization error statistics
5. Drift patterns and anchor effectiveness

Arguments:
    log_file_path: Path to GONet log file (can be 1GB+)
    --gonetid: Optional filter for specific GONetId (analyzes all if not specified)

Example:
    python analyze_quantization_anchoring.py gonet-2025-10-26.log
    python analyze_quantization_anchoring.py gonet-2025-10-26.log --gonetid 5119
"""

import re
import sys
from collections import defaultdict
from dataclasses import dataclass
from typing import Dict, List, Optional


@dataclass
class QuantCheckEvent:
    """Represents a [QUANT-CHECK] log entry (Phase 2 format with per-component errors)."""
    timestamp: str
    gonetid: int
    idx: int
    value_type: str  # Vector3, Quaternion, Vector2, Vector4, float
    threshold: float
    time_since_anchor: float
    max_time: float
    all_pass: bool  # allPass flag (Phase 1 logic - all components must pass)
    # Per-component data (for vectors)
    error_x: Optional[float] = None
    error_y: Optional[float] = None
    error_z: Optional[float] = None
    error_w: Optional[float] = None
    moving_x: Optional[bool] = None
    moving_y: Optional[bool] = None
    moving_z: Optional[bool] = None
    moving_w: Optional[bool] = None
    # Single value data (for float)
    error: Optional[float] = None
    near_boundary: Optional[bool] = None


@dataclass
class AnchorEvent:
    """Represents an anchor event (quantization or fallback)."""
    timestamp: str
    gonetid: int
    idx: int
    value_type: str
    anchor_type: str  # "QUANTIZATION" or "FALLBACK"
    quant_error: Optional[float] = None
    time_since_anchor: Optional[float] = None


class QuantizationAnchoringAnalyzer:
    def __init__(self, log_file: str, filter_gonetid: Optional[int] = None):
        self.log_file = log_file
        self.filter_gonetid = filter_gonetid

        # Statistics
        self.quant_checks: List[QuantCheckEvent] = []
        self.anchors: List[AnchorEvent] = []
        self.velocity_bundles_by_gonetid: Dict[int, int] = defaultdict(int)
        self.value_bundles_by_gonetid: Dict[int, int] = defaultdict(int)

        # Regex patterns (Phase 2 format with per-component errors and motion detection)

        # Vector3 QUANT-CHECK: errorX errorY errorZ threshold delta moving checks allPass timeSinceAnchor
        self.quant_check_vector3_pattern = re.compile(
            r'\[VelocitySync\]\[QUANT-CHECK\] GONetId:(\d+) idx:(\d+) '
            r'type:Vector3 errorX:([\d.]+) errorY:([\d.]+) errorZ:([\d.]+) threshold:([\d.]+) '
            r'delta:\([^)]+\) motionEps:[\d.]+ '
            r'moving:\(x:(\w+) y:(\w+) z:(\w+)\) '
            r'checks:\([^)]+\) allPass:(\w+) '
            r'timeSinceAnchor:([\d.]+)s maxTime:([\d.]+)s'
        )

        # Vector2 QUANT-CHECK
        self.quant_check_vector2_pattern = re.compile(
            r'\[VelocitySync\]\[QUANT-CHECK\] GONetId:(\d+) idx:(\d+) '
            r'type:Vector2 errorX:([\d.]+) errorY:([\d.]+) threshold:([\d.]+) '
            r'delta:\([^)]+\) motionEps:[\d.]+ '
            r'moving:\(x:(\w+) y:(\w+)\) '
            r'checks:\([^)]+\) allPass:(\w+) '
            r'timeSinceAnchor:([\d.]+)s maxTime:([\d.]+)s'
        )

        # Vector4 QUANT-CHECK
        self.quant_check_vector4_pattern = re.compile(
            r'\[VelocitySync\]\[QUANT-CHECK\] GONetId:(\d+) idx:(\d+) '
            r'type:Vector4 errorX:([\d.]+) errorY:([\d.]+) errorZ:([\d.]+) errorW:([\d.]+) threshold:([\d.]+) '
            r'delta:\([^)]+\) motionEps:[\d.]+ '
            r'moving:\(x:(\w+) y:(\w+) z:(\w+) w:(\w+)\) '
            r'checks:\([^)]+\) allPass:(\w+) '
            r'timeSinceAnchor:([\d.]+)s maxTime:([\d.]+)s'
        )

        # float QUANT-CHECK
        self.quant_check_float_pattern = re.compile(
            r'\[VelocitySync\]\[QUANT-CHECK\] GONetId:(\d+) idx:(\d+) '
            r'type:float error:([\d.]+) threshold:([\d.]+) nearBoundary:(\w+) '
            r'timeSinceAnchor:([\d.]+)s maxTime:([\d.]+)s'
        )

        # ANCHOR-QUANTIZATION (Phase 2 format with moving components)
        self.anchor_quant_pattern = re.compile(
            r'\[VelocitySync\]\[ANCHOR-QUANTIZATION\] GONetId:(\d+) idx:(\d+) '
            r'type:(\w+) '
        )

        # ANCHOR-FALLBACK
        self.anchor_fallback_pattern = re.compile(
            r'\[VelocitySync\]\[ANCHOR-FALLBACK\] GONetId:(\d+) idx:(\d+) '
            r'type:(\w+) timeSinceAnchor:([\d.]+)s'
        )

        # VELOCITY bundles have GONetId in the log message
        self.velocity_bundle_pattern = re.compile(
            r'\[SERVER-SEND-VEL\] GONetId:(\d+)'
        )

    def parse_log(self):
        """Parse log file and extract relevant events."""
        print(f"[*] Parsing log file: {self.log_file}")
        print(f"    (This may take a while for large files...)")

        line_count = 0
        with open(self.log_file, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                line_count += 1
                if line_count % 1_000_000 == 0:
                    print(f"   Processed {line_count // 1_000_000}M lines...")

                # Extract timestamp (first part of line)
                timestamp_match = re.match(r'(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})', line)
                timestamp = timestamp_match.group(1) if timestamp_match else "UNKNOWN"

                # Parse QUANT-CHECK events (Vector3)
                match = self.quant_check_vector3_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = QuantCheckEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type="Vector3",
                            error_x=float(match.group(3)),
                            error_y=float(match.group(4)),
                            error_z=float(match.group(5)),
                            threshold=float(match.group(6)),
                            moving_x=match.group(7) == "True",
                            moving_y=match.group(8) == "True",
                            moving_z=match.group(9) == "True",
                            all_pass=match.group(10) == "True",
                            time_since_anchor=float(match.group(11)),
                            max_time=float(match.group(12))
                        )
                        self.quant_checks.append(event)
                    continue

                # Parse QUANT-CHECK events (Vector2)
                match = self.quant_check_vector2_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = QuantCheckEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type="Vector2",
                            error_x=float(match.group(3)),
                            error_y=float(match.group(4)),
                            threshold=float(match.group(5)),
                            moving_x=match.group(6) == "True",
                            moving_y=match.group(7) == "True",
                            all_pass=match.group(8) == "True",
                            time_since_anchor=float(match.group(9)),
                            max_time=float(match.group(10))
                        )
                        self.quant_checks.append(event)
                    continue

                # Parse QUANT-CHECK events (Vector4)
                match = self.quant_check_vector4_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = QuantCheckEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type="Vector4",
                            error_x=float(match.group(3)),
                            error_y=float(match.group(4)),
                            error_z=float(match.group(5)),
                            error_w=float(match.group(6)),
                            threshold=float(match.group(7)),
                            moving_x=match.group(8) == "True",
                            moving_y=match.group(9) == "True",
                            moving_z=match.group(10) == "True",
                            moving_w=match.group(11) == "True",
                            all_pass=match.group(12) == "True",
                            time_since_anchor=float(match.group(13)),
                            max_time=float(match.group(14))
                        )
                        self.quant_checks.append(event)
                    continue

                # Parse QUANT-CHECK events (float)
                match = self.quant_check_float_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = QuantCheckEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type="float",
                            error=float(match.group(3)),
                            threshold=float(match.group(4)),
                            near_boundary=match.group(5) == "True",
                            time_since_anchor=float(match.group(6)),
                            max_time=float(match.group(7)),
                            all_pass=match.group(5) == "True"  # nearBoundary is equivalent to allPass for float
                        )
                        self.quant_checks.append(event)
                    continue

                # Parse ANCHOR-QUANTIZATION events
                match = self.anchor_quant_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = AnchorEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type=match.group(3),
                            anchor_type="QUANTIZATION"
                        )
                        self.anchors.append(event)
                    continue

                # Parse ANCHOR-FALLBACK events
                match = self.anchor_fallback_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = AnchorEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type=match.group(3),
                            anchor_type="FALLBACK",
                            time_since_anchor=float(match.group(4))
                        )
                        self.anchors.append(event)
                    continue

                # Parse VELOCITY bundle events (with GONetId filtering)
                match = self.velocity_bundle_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        self.velocity_bundles_by_gonetid[gonetid] += 1
                    continue

        print(f"[OK] Parsing complete! Processed {line_count:,} lines")

    def generate_report(self):
        """Generate comprehensive analysis report."""
        print("\n" + "="*80)
        print("[REPORT] QUANTIZATION-AWARE ANCHORING ANALYSIS")
        print("="*80)

        if self.filter_gonetid:
            print(f"\n[FILTER] GONetId: {self.filter_gonetid}")
        else:
            print(f"\n[FILTER] All GONetIds")

        # Section 1: Overall Statistics
        print("\n" + "-"*80)
        print("1. OVERALL STATISTICS")
        print("-"*80)

        total_quant_checks = len(self.quant_checks)
        total_quant_anchors = sum(1 for a in self.anchors if a.anchor_type == "QUANTIZATION")
        total_fallback_anchors = sum(1 for a in self.anchors if a.anchor_type == "FALLBACK")
        total_anchors = len(self.anchors)

        print(f"  Quantization checks:        {total_quant_checks:,}")
        print(f"  Quantization-aware anchors: {total_quant_anchors:,} ({total_quant_anchors/max(1,total_anchors)*100:.1f}% of anchors)")
        print(f"  Time-based fallback anchors:{total_fallback_anchors:,} ({total_fallback_anchors/max(1,total_anchors)*100:.1f}% of anchors)")
        print(f"  Total VALUE anchors:        {total_anchors:,}")

        # Section 2: Bundle Statistics
        print("\n" + "-"*80)
        print("2. VELOCITY vs VALUE BUNDLE STATISTICS")
        print("-"*80)

        # Calculate VELOCITY bundle count (sum all GONetIds or specific filtered one)
        if self.filter_gonetid:
            velocity_count = self.velocity_bundles_by_gonetid.get(self.filter_gonetid, 0)
        else:
            velocity_count = sum(self.velocity_bundles_by_gonetid.values())

        # VALUE bundles = anchors (QUANTIZATION + FALLBACK)
        value_count = total_anchors
        total = velocity_count + value_count

        if total > 0:
            velocity_pct = velocity_count / total * 100
            value_pct = value_count / total * 100
            print(f"  VELOCITY bundles: {velocity_count:,} ({velocity_pct:.1f}%)")
            print(f"  VALUE bundles:    {value_count:,} ({value_pct:.1f}%)")
            print(f"  Total bundles:    {total:,}")
        else:
            print("  No bundle data found in log file.")

        # Section 3: Quantization Error Distribution (Per-Component Analysis)
        print("\n" + "-"*80)
        print("3. QUANTIZATION ERROR DISTRIBUTION (PHASE 2: PER-COMPONENT)")
        print("-"*80)

        if self.quant_checks:
            thresholds = [c.threshold for c in self.quant_checks]
            print(f"  Mean threshold:  {sum(thresholds)/len(thresholds):.6f}")

            # Vector3 per-component stats
            vector3_checks = [c for c in self.quant_checks if c.value_type == "Vector3"]
            if vector3_checks:
                errors_x = [c.error_x for c in vector3_checks]
                errors_y = [c.error_y for c in vector3_checks]
                errors_z = [c.error_z for c in vector3_checks]

                print(f"\n  Vector3 Component Errors:")
                print(f"    X: min={min(errors_x):.6f} max={max(errors_x):.6f} mean={sum(errors_x)/len(errors_x):.6f}")
                print(f"    Y: min={min(errors_y):.6f} max={max(errors_y):.6f} mean={sum(errors_y)/len(errors_y):.6f}")
                print(f"    Z: min={min(errors_z):.6f} max={max(errors_z):.6f} mean={sum(errors_z)/len(errors_z):.6f}")

                # Motion detection stats
                moving_x_count = sum(1 for c in vector3_checks if c.moving_x)
                moving_y_count = sum(1 for c in vector3_checks if c.moving_y)
                moving_z_count = sum(1 for c in vector3_checks if c.moving_z)

                print(f"\n  Vector3 Motion Detection:")
                print(f"    X moving: {moving_x_count:,} ({moving_x_count/len(vector3_checks)*100:.1f}%)")
                print(f"    Y moving: {moving_y_count:,} ({moving_y_count/len(vector3_checks)*100:.1f}%)")
                print(f"    Z moving: {moving_z_count:,} ({moving_z_count/len(vector3_checks)*100:.1f}%)")

            # Phase 1 logic: All components must pass
            all_pass_count = sum(1 for c in self.quant_checks if c.all_pass)
            print(f"\n  Phase 1 Logic (ALL components must pass):")
            print(f"    Checks with allPass=True: {all_pass_count:,} ({all_pass_count/len(self.quant_checks)*100:.1f}%)")

        # Section 4: Anchor Timing Analysis
        print("\n" + "-"*80)
        print("4. ANCHOR TIMING ANALYSIS")
        print("-"*80)

        if self.quant_checks:
            times = [c.time_since_anchor for c in self.quant_checks]
            print(f"  Min time since last anchor: {min(times):.3f}s")
            print(f"  Max time since last anchor: {max(times):.3f}s")
            print(f"  Mean time since anchor:     {sum(times)/len(times):.3f}s")

            # Fallback anchor timing
            fallback_times = [a.time_since_anchor for a in self.anchors if a.anchor_type == "FALLBACK" and a.time_since_anchor]
            if fallback_times:
                print(f"\n  Fallback anchor trigger times:")
                print(f"    Min: {min(fallback_times):.3f}s")
                print(f"    Max: {max(fallback_times):.3f}s")
                print(f"    Mean: {sum(fallback_times)/len(fallback_times):.3f}s")

        # Section 5: Value Type Breakdown
        print("\n" + "-"*80)
        print("5. VALUE TYPE BREAKDOWN")
        print("-"*80)

        type_counts = defaultdict(int)
        for check in self.quant_checks:
            type_counts[check.value_type] += 1

        for value_type, count in sorted(type_counts.items()):
            print(f"  {value_type}: {count:,} checks")

        # Section 6: Success Rate Analysis (Phase 2: Moving Component Logic)
        print("\n" + "-"*80)
        print("6. PHASE 2 MOVING-COMPONENT LOGIC ANALYSIS")
        print("-"*80)

        if self.quant_checks:
            # Simulate Phase 2 logic for Vector3
            vector3_checks = [c for c in self.quant_checks if c.value_type == "Vector3"]
            if vector3_checks:
                phase2_would_pass = 0
                for c in vector3_checks:
                    # Phase 2 logic: Only check moving components
                    all_moving_pass = True
                    any_moving = False

                    if c.moving_x:
                        any_moving = True
                        if c.error_x >= c.threshold:
                            all_moving_pass = False
                    if c.moving_y:
                        any_moving = True
                        if c.error_y >= c.threshold:
                            all_moving_pass = False
                    if c.moving_z:
                        any_moving = True
                        if c.error_z >= c.threshold:
                            all_moving_pass = False

                    if all_moving_pass and any_moving:
                        phase2_would_pass += 1

                print(f"  Vector3 Phase 2 Simulation:")
                print(f"    Checks that WOULD trigger quant anchor (moving-component logic):")
                print(f"      {phase2_would_pass:,} / {len(vector3_checks):,} ({phase2_would_pass/len(vector3_checks)*100:.1f}%)")
                print(f"    vs Phase 1 (all-component logic): {all_pass_count:,} ({all_pass_count/len(self.quant_checks)*100:.1f}%)")
                print(f"    Improvement: +{phase2_would_pass - all_pass_count:,} opportunities ({(phase2_would_pass - all_pass_count)/len(vector3_checks)*100:.1f}% increase)")

        # Section 7: Recommendations (Phase 2)
        print("\n" + "-"*80)
        print("7. ANALYSIS & RECOMMENDATIONS (PHASE 2)")
        print("-"*80)

        if self.quant_checks:
            total_bundles = velocity_count + value_count
            velocity_ratio = velocity_count / max(1, total_bundles)
            quant_anchor_ratio = total_quant_anchors / max(1, total_anchors)
            fallback_ratio = total_fallback_anchors / max(1, total_anchors)

            print(f"\n  [CHECK] VELOCITY bundle ratio: {velocity_ratio*100:.1f}% (target: >90% for SLOW preset)")
            if velocity_ratio > 0.9:
                print(f"     [SUCCESS] System correctly using VELOCITY bundles for sub-quantization motion!")
            else:
                print(f"     [WARNING] Expected higher VELOCITY ratio for SLOW movement preset")

            print(f"\n  [CHECK] Quantization-aware anchor ratio: {quant_anchor_ratio*100:.1f}% (target: >40% Phase 2)")
            if quant_anchor_ratio > 0.4:
                print(f"     [SUCCESS] Phase 2 moving-component logic working! Most anchors are smart!")
            elif quant_anchor_ratio == 0:
                print(f"     [WARNING] 0% quantization anchors - Phase 2 may not be running or no clean moments found")
            else:
                print(f"     [NOTE] Some quantization anchors working, but could be improved")

            print(f"\n  [CHECK] Fallback anchor ratio: {fallback_ratio*100:.1f}% (target: 40-60% Phase 2)")
            if 0.4 <= fallback_ratio <= 0.6:
                print(f"     [SUCCESS] Good balance! Quantization anchors working, fallback safety net intact!")
            elif fallback_ratio < 0.4:
                print(f"     [EXCELLENT] Very high quantization anchor rate! Most anchors are smart!")
            else:
                print(f"     [NOTE] Many fallback anchors - motion patterns may not align with quantization grid")

            # Phase 2 specific analysis
            if vector3_checks:
                moving_y_rate = sum(1 for c in vector3_checks if c.moving_y) / len(vector3_checks)
                if moving_y_rate < 0.1:
                    print(f"\n  [PHASE 2 INSIGHT] Y component mostly stationary ({(1-moving_y_rate)*100:.0f}% not moving)")
                    print(f"     This is why Phase 2 helps - excludes stationary Y from boundary checks!")

        print("\n" + "="*80)
        print("[DONE] ANALYSIS COMPLETE")
        print("="*80 + "\n")


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    log_file = sys.argv[1]
    filter_gonetid = None

    # Parse optional --gonetid argument
    if len(sys.argv) >= 4 and sys.argv[2] == '--gonetid':
        try:
            filter_gonetid = int(sys.argv[3])
        except ValueError:
            print(f"[ERROR] Invalid GONetId '{sys.argv[3]}' (must be integer)")
            sys.exit(1)

    # Run analysis
    analyzer = QuantizationAnchoringAnalyzer(log_file, filter_gonetid)
    analyzer.parse_log()
    analyzer.generate_report()


if __name__ == "__main__":
    main()
