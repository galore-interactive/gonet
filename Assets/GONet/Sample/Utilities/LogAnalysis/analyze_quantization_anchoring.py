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
    """Represents a [QUANT-CHECK] log entry."""
    timestamp: str
    gonetid: int
    idx: int
    value_type: str  # Vector3, Quaternion, Vector2, Vector4, float
    quant_error: float
    quant_step: float
    threshold: float
    time_since_anchor: float
    max_time: float
    quant_anchor: bool
    fallback_anchor: bool


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

        # Regex patterns
        self.quant_check_pattern = re.compile(
            r'\[VelocitySync\]\[QUANT-CHECK\] GONetId:(\d+) idx:(\d+) '
            r'type:(\w+) quantError:([\d.]+) quantStep:([\d.]+) threshold:([\d.]+) '
            r'timeSinceAnchor:([\d.]+)s maxTime:([\d.]+)s '
            r'quantAnchor:(\w+) fallbackAnchor:(\w+)'
        )

        self.anchor_quant_pattern = re.compile(
            r'\[VelocitySync\]\[ANCHOR-QUANTIZATION\] GONetId:(\d+) idx:(\d+) '
            r'type:(\w+) quantError:([\d.]+) → Smart anchor'
        )

        self.anchor_fallback_pattern = re.compile(
            r'\[VelocitySync\]\[ANCHOR-FALLBACK\] GONetId:(\d+) idx:(\d+) '
            r'type:(\w+) timeSinceAnchor:([\d.]+)s → Fallback anchor'
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

                # Parse QUANT-CHECK events
                match = self.quant_check_pattern.search(line)
                if match:
                    gonetid = int(match.group(1))
                    if self.filter_gonetid is None or gonetid == self.filter_gonetid:
                        event = QuantCheckEvent(
                            timestamp=timestamp,
                            gonetid=gonetid,
                            idx=int(match.group(2)),
                            value_type=match.group(3),
                            quant_error=float(match.group(4)),
                            quant_step=float(match.group(5)),
                            threshold=float(match.group(6)),
                            time_since_anchor=float(match.group(7)),
                            max_time=float(match.group(8)),
                            quant_anchor=match.group(9) == "True",
                            fallback_anchor=match.group(10) == "True"
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
                            anchor_type="QUANTIZATION",
                            quant_error=float(match.group(4))
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

        # Section 3: Quantization Error Distribution
        print("\n" + "-"*80)
        print("3. QUANTIZATION ERROR DISTRIBUTION")
        print("-"*80)

        if self.quant_checks:
            errors = [c.quant_error for c in self.quant_checks]
            thresholds = [c.threshold for c in self.quant_checks]

            print(f"  Min error:       {min(errors):.6f}")
            print(f"  Max error:       {max(errors):.6f}")
            print(f"  Mean error:      {sum(errors)/len(errors):.6f}")
            print(f"  Mean threshold:  {sum(thresholds)/len(thresholds):.6f}")

            # Distribution buckets
            below_threshold = sum(1 for c in self.quant_checks if c.quant_error < c.threshold)
            print(f"\n  Error distribution:")
            print(f"    Below threshold (triggers quant anchor): {below_threshold:,} ({below_threshold/len(errors)*100:.1f}%)")
            print(f"    Above threshold (no quant anchor):      {len(errors)-below_threshold:,} ({(len(errors)-below_threshold)/len(errors)*100:.1f}%)")

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

        # Section 6: Success Rate Analysis
        print("\n" + "-"*80)
        print("6. QUANTIZATION-AWARE ANCHORING SUCCESS RATE")
        print("-"*80)

        if self.quant_checks:
            triggered_quant = sum(1 for c in self.quant_checks if c.quant_anchor)
            triggered_fallback = sum(1 for c in self.quant_checks if c.fallback_anchor)

            print(f"  Checks that triggered quant anchor:   {triggered_quant:,} ({triggered_quant/len(self.quant_checks)*100:.2f}%)")
            print(f"  Checks that triggered fallback anchor:{triggered_fallback:,} ({triggered_fallback/len(self.quant_checks)*100:.2f}%)")
            print(f"  Checks with no anchor:                 {len(self.quant_checks)-triggered_quant-triggered_fallback:,}")

        # Section 7: Recommendations
        print("\n" + "-"*80)
        print("7. ANALYSIS & RECOMMENDATIONS")
        print("-"*80)

        if self.quant_checks:
            total_bundles = velocity_count + value_count
            velocity_ratio = velocity_count / max(1, total_bundles)
            quant_anchor_ratio = total_quant_anchors / max(1, total_anchors)
            fallback_ratio = total_fallback_anchors / max(1, total_anchors)

            print(f"\n  [CHECK] VELOCITY bundle ratio: {velocity_ratio*100:.1f}% (target: >90% for SLOW preset)")
            if velocity_ratio > 0.9:
                print(f"     SUCCESS: System correctly using VELOCITY bundles for sub-quantization motion!")
            else:
                print(f"     WARNING: Expected higher VELOCITY ratio for SLOW movement preset")

            print(f"\n  [CHECK] Quantization-aware anchor ratio: {quant_anchor_ratio*100:.1f}% (target: >70%)")
            if quant_anchor_ratio > 0.7:
                print(f"     SUCCESS: Most anchors are quantization-aware (clean boundaries)!")
            else:
                print(f"     WARNING: Too many fallback anchors - may need threshold adjustment")

            print(f"\n  [CHECK] Fallback anchor ratio: {fallback_ratio*100:.1f}% (target: <30%)")
            if fallback_ratio < 0.3:
                print(f"     SUCCESS: Fallback safety net rarely needed!")
            else:
                print(f"     NOTE: Fallback anchors firing frequently - consider longer VelocityAnchorIntervalSeconds")

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
