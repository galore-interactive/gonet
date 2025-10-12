#!/usr/bin/env python3
"""
analyze_ongonetready_timing.py
Analyzes OnGONetReady timing for all GONetParticipants (client and server)

Usage: python3 analyze_ongonetready_timing.py <logfile>
"""

import sys
import re
from collections import defaultdict, Counter

def parse_log(logfile):
    """Parse log file and extract Start() and OnGONetReady FIRED events"""

    start_events = []  # [(peer, gonetid, frame, gameobject)]
    ready_events = []  # [(peer, gonetid, frame)]

    with open(logfile, 'r', encoding='utf-8', errors='ignore') as f:
        for line in f:
            # Extract Start() events
            if 'Start() called' in line:
                peer_match = re.search(r'\[(Client:\d+|Server)\]', line)
                frame_match = re.search(r'frame:(\d+)/', line)
                gonetid_match = re.search(r'GONetId: (\d+)', line)
                gameobject_match = re.search(r'GameObject: ([^,]+)', line)

                if peer_match and frame_match and gonetid_match and gameobject_match:
                    start_events.append((
                        peer_match.group(1),
                        int(gonetid_match.group(1)),
                        int(frame_match.group(1)),
                        gameobject_match.group(1).strip()
                    ))

            # Extract OnGONetReady FIRED events
            elif 'OnGONetReady FIRED' in line:
                peer_match = re.search(r'\[(Client:\d+|Server)\]', line)
                frame_match = re.search(r'frame:(\d+)/', line)
                gonetid_match = re.search(r'GONetId: (\d+)', line)

                if peer_match and frame_match and gonetid_match:
                    ready_events.append((
                        peer_match.group(1),
                        int(gonetid_match.group(1)),
                        int(frame_match.group(1))
                    ))

    return start_events, ready_events

def join_events(start_events, ready_events):
    """Join Start and Ready events to calculate frame delays"""

    # Create dict for quick lookup of ready events
    ready_dict = {}
    for peer, gonetid, frame in ready_events:
        key = (peer, gonetid)
        if key not in ready_dict:
            ready_dict[key] = frame

    results = []
    for peer, gonetid, start_frame, gameobject in start_events:
        key = (peer, gonetid)
        if key in ready_dict:
            ready_frame = ready_dict[key]
            frame_delay = ready_frame - start_frame
            results.append((peer, gonetid, gameobject, start_frame, ready_frame, frame_delay))
        else:
            results.append((peer, gonetid, gameobject, start_frame, None, None))

    return results

def calculate_stats(results):
    """Calculate statistics by peer"""

    stats_by_peer = defaultdict(lambda: {
        'total': 0,
        'fired': 0,
        'never': 0,
        'delays': [],
        'never_fired': []
    })

    for peer, gonetid, gameobject, start_frame, ready_frame, frame_delay in results:
        stats = stats_by_peer[peer]
        stats['total'] += 1

        if frame_delay is not None:
            stats['fired'] += 1
            stats['delays'].append(frame_delay)
        else:
            stats['never'] += 1
            stats['never_fired'].append((gonetid, gameobject, start_frame))

    return stats_by_peer

def calculate_gameobject_stats(results):
    """Calculate statistics grouped by GameObject type"""

    stats_by_type = defaultdict(lambda: {
        'fired': 0,
        'never': 0,
        'delays': []
    })

    for peer, gonetid, gameobject, start_frame, ready_frame, frame_delay in results:
        # Remove (Clone) suffix
        name = gameobject.replace('(Clone)', '').strip()

        if frame_delay is not None:
            stats_by_type[name]['fired'] += 1
            stats_by_type[name]['delays'].append(frame_delay)
        else:
            stats_by_type[name]['never'] += 1

    return stats_by_type

def print_stats(stats_by_peer):
    """Print summary statistics by peer"""

    print("=" * 60)
    print("SUMMARY BY PEER")
    print("=" * 60)
    print()

    for peer in ['Server', 'Client:1']:
        if peer not in stats_by_peer:
            print(f"--- {peer} ---")
            print("No participants found")
            print()
            continue

        stats = stats_by_peer[peer]
        print(f"--- {peer} ---")
        print(f"Total participants: {stats['total']}")
        print(f"[OK] OnGONetReady fired: {stats['fired']} ({100.0 * stats['fired'] / stats['total']:.1f}%)")
        print(f"[!!] OnGONetReady NEVER fired: {stats['never']}")

        if stats['delays']:
            delays = stats['delays']
            avg_delay = sum(delays) / len(delays)
            min_delay = min(delays)
            max_delay = max(delays)

            print()
            print(f"Average frame delay: {avg_delay:.2f} frames")
            print(f"Min frame delay: {min_delay} frame(s)")
            print(f"Max frame delay: {max_delay} frame(s)")

            # Delay distribution
            print()
            print("Frame delay distribution:")
            delay_counts = Counter(delays)
            for delay in sorted(delay_counts.keys()):
                count = delay_counts[delay]
                pct = 100.0 * count / len(delays)
                print(f"  {delay:2d} frames: {count:4d} participants ({pct:5.1f}%)")

        print()

def print_never_fired(stats_by_peer):
    """Print participants that never fired OnGONetReady"""

    print("=" * 60)
    print("PARTICIPANTS THAT NEVER FIRED OnGONetReady")
    print("=" * 60)
    print()

    never_total = sum(stats['never'] for stats in stats_by_peer.values())

    if never_total > 0:
        print(f"Found {never_total} participants that NEVER fired OnGONetReady:")
        print()
        for peer, stats in stats_by_peer.items():
            for gonetid, gameobject, start_frame in stats['never_fired']:
                print(f"[{peer}] GONetId: {gonetid}, GameObject: {gameobject}, StartFrame: {start_frame}")
    else:
        print("[OK] ALL participants successfully fired OnGONetReady!")

    print()

def print_gameobject_stats(stats_by_type):
    """Print statistics grouped by GameObject type"""

    print("=" * 60)
    print("BREAKDOWN BY GAMEOBJECT TYPE")
    print("=" * 60)
    print()

    for name in sorted(stats_by_type.keys()):
        stats = stats_by_type[name]
        print(f"{name}:")

        if stats['delays']:
            avg_delay = sum(stats['delays']) / len(stats['delays'])
            min_delay = min(stats['delays'])
            max_delay = max(stats['delays'])
            print(f"  [OK] Fired: {stats['fired']}, [!!] Never: {stats['never']}, Avg: {avg_delay:.2f} frames, Min: {min_delay}, Max: {max_delay}")
        else:
            print(f"  [OK] Fired: 0, [!!] Never: {stats['never']}")

        print()

def main():
    logfile = sys.argv[1] if len(sys.argv) > 1 else \
        "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-10-11.log"

    print("=" * 60)
    print("OnGONetReady Timing Analysis")
    print("=" * 60)
    print(f"Log file: {logfile}")
    print()

    start_events, ready_events = parse_log(logfile)
    print(f"Extracted {len(start_events)} Start() events")
    print(f"Extracted {len(ready_events)} OnGONetReady FIRED events")
    print()

    results = join_events(start_events, ready_events)
    stats_by_peer = calculate_stats(results)
    stats_by_type = calculate_gameobject_stats(results)

    print_stats(stats_by_peer)
    print_never_fired(stats_by_peer)
    print_gameobject_stats(stats_by_type)

    # Print sample results
    print("=" * 60)
    print("DETAILED RESULTS SAMPLE")
    print("=" * 60)
    print()
    print("First 20 entries:")
    print(f"{'Peer':<12} {'GONetId':<10} {'GameObject':<30} {'StartFrame':<12} {'ReadyFrame':<12} {'FrameDelay':<12}")
    print("-" * 98)
    for i, (peer, gonetid, gameobject, start_frame, ready_frame, frame_delay) in enumerate(results[:20]):
        ready_str = str(ready_frame) if ready_frame is not None else "NEVER"
        delay_str = str(frame_delay) if frame_delay is not None else "NEVER"
        print(f"{peer:<12} {gonetid:<10} {gameobject:<30} {start_frame:<12} {ready_str:<12} {delay_str:<12}")
    print()

if __name__ == '__main__':
    main()
