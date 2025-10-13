#!/usr/bin/env python3
"""
GONet Spawn Propagation Analysis Script

PURPOSE:
    Analyzes spawn event propagation across server and multiple clients.
    Identifies spawns that failed to propagate from one peer to another.
    Tracks missing GONetIds and correlates with queue backup warnings.

USAGE:
    python3 analyze_spawn_propagation.py <event-log-directory>

    Example:
    python3 analyze_spawn_propagation.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs"

OUTPUT:
    - Summary of spawn events per peer
    - List of GONetIds that appear in some peers but not others
    - Timeline of spawn failures
    - Correlation with queue backup warnings
"""

import re
import sys
from collections import defaultdict
from pathlib import Path

def parse_event_log(filepath):
    """Parse an event log file and extract spawn events."""
    spawns = {}  # GONetId -> event details
    peer_role = None
    authority_id = None

    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

        # Extract peer role and authority ID from header
        role_match = re.search(r'Role: (\w+)', content)
        authority_match = re.search(r'Authority ID: (\d+)', content)

        if role_match:
            peer_role = role_match.group(1)
        if authority_match:
            authority_id = int(authority_match.group(1))

        # Parse spawn events
        # Pattern: [Event NNNNNN] Type=InstantiateGONetParticipantEvent
        #          Timestamp: Ticks=...
        #          GONetId: XXXXX
        #          Owner: AuthorityNNN
        #          Details: ... DesignTimeLocation=... Position=... Rotation=...

        event_pattern = re.compile(
            r'\[Event (\d+)\] Type=InstantiateGONetParticipantEvent.*?'
            r'GONetId: (\d+).*?'
            r'Owner: Authority(\d+).*?'
            r'Details: .*?DesignTimeLocation=([\w:/\.]+)',
            re.DOTALL
        )

        for match in event_pattern.finditer(content):
            event_num = int(match.group(1))
            gonet_id = int(match.group(2))
            owner_authority = int(match.group(3))
            location = match.group(4)

            spawns[gonet_id] = {
                'event_num': event_num,
                'owner': owner_authority,
                'location': location
            }

    return {
        'peer_role': peer_role,
        'authority_id': authority_id,
        'spawns': spawns
    }

def parse_main_log_for_queue_warnings(filepath):
    """Parse main log file for queue backup warnings."""
    warnings = []

    with open(filepath, 'r', encoding='utf-8') as f:
        for line in f:
            if 'QUEUE-BACKUP' in line or 'messageQueue depth' in line:
                # Extract timestamp and client
                match = re.search(r'\[(Client:\d+)\].*?(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})', line)
                if match:
                    warnings.append({
                        'client': match.group(1),
                        'timestamp': match.group(2),
                        'message': line.strip()
                    })

    return warnings

def analyze_spawn_propagation(log_directory):
    """Main analysis function."""
    log_dir = Path(log_directory)

    # Find all event log files
    event_logs = list(log_dir.glob('gonet-events-*-*.txt'))

    if not event_logs:
        print(f"ERROR: No event log files found in {log_directory}")
        return

    print("=" * 80)
    print("GONet Spawn Propagation Analysis")
    print("=" * 80)
    print()

    # Parse all event logs
    peers = {}
    for log_file in sorted(event_logs):
        peer_data = parse_event_log(log_file)
        peer_name = f"{peer_data['peer_role']}"
        if peer_data['authority_id'] is not None and peer_data['peer_role'] != 'Server':
            peer_name = f"{peer_data['peer_role']}{peer_data['authority_id']}"

        peers[peer_name] = peer_data
        print(f"Loaded {log_file.name}: {peer_name} (Authority {peer_data['authority_id']}) - {len(peer_data['spawns'])} spawns")

    print()
    print("-" * 80)
    print("SPAWN COUNT BY PEER")
    print("-" * 80)

    for peer_name in sorted(peers.keys()):
        peer_data = peers[peer_name]
        print(f"{peer_name:20s}: {len(peer_data['spawns']):5d} spawns")

    print()
    print("-" * 80)
    print("ANALYZING PROPAGATION FAILURES")
    print("-" * 80)
    print()

    # Collect all GONetIds across all peers
    all_gonet_ids = set()
    for peer_data in peers.values():
        all_gonet_ids.update(peer_data['spawns'].keys())

    print(f"Total unique GONetIds across all peers: {len(all_gonet_ids)}")
    print()

    # Find GONetIds that don't appear in all peers
    missing_by_peer = defaultdict(list)

    for gonet_id in sorted(all_gonet_ids):
        peers_with_id = []
        peers_without_id = []

        for peer_name, peer_data in peers.items():
            if gonet_id in peer_data['spawns']:
                peers_with_id.append(peer_name)
            else:
                peers_without_id.append(peer_name)

        # If not all peers have this GONetId, it's a propagation failure
        if peers_without_id:
            for peer_name in peers_without_id:
                missing_by_peer[peer_name].append({
                    'gonet_id': gonet_id,
                    'present_in': peers_with_id
                })

    if not missing_by_peer:
        print("[OK] SUCCESS: All spawns propagated to all peers!")
    else:
        print("[!!] PROPAGATION FAILURES DETECTED:")
        print()

        for peer_name in sorted(missing_by_peer.keys()):
            missing_ids = missing_by_peer[peer_name]
            print(f"  {peer_name} is MISSING {len(missing_ids)} spawns:")

            # Group by originating peer
            by_origin = defaultdict(list)
            for item in missing_ids:
                # Determine who spawned it (check which peer has it first)
                origin = item['present_in'][0] if item['present_in'] else 'Unknown'
                by_origin[origin].append(item['gonet_id'])

            for origin_peer in sorted(by_origin.keys()):
                ids = by_origin[origin_peer]
                print(f"    From {origin_peer}: {len(ids)} missing")

                # Show first 10 and last 10 GONetIds
                if len(ids) <= 20:
                    print(f"      GONetIds: {ids}")
                else:
                    print(f"      GONetIds: {ids[:10]} ... {ids[-10:]}")

            print()

    # Parse main log for queue warnings
    main_log = log_dir / 'gonet-2025-10-13.log'
    if main_log.exists():
        print("-" * 80)
        print("QUEUE BACKUP WARNINGS")
        print("-" * 80)
        print()

        warnings = parse_main_log_for_queue_warnings(main_log)

        if warnings:
            by_client = defaultdict(list)
            for warning in warnings:
                by_client[warning['client']].append(warning)

            for client in sorted(by_client.keys()):
                client_warnings = by_client[client]
                print(f"{client}: {len(client_warnings)} warnings")
                for warning in client_warnings[:5]:  # Show first 5
                    print(f"  {warning['timestamp']}: {warning['message'][:100]}...")
                if len(client_warnings) > 5:
                    print(f"  ... and {len(client_warnings) - 5} more warnings")
                print()
        else:
            print("No queue backup warnings found in main log.")

    print("-" * 80)
    print("ANALYSIS COMPLETE")
    print("-" * 80)

if __name__ == '__main__':
    if len(sys.argv) < 2:
        # Default path
        log_directory = "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs"
        print(f"Using default log directory: {log_directory}")
    else:
        log_directory = sys.argv[1]

    try:
        analyze_spawn_propagation(log_directory)
    except Exception as e:
        print(f"ERROR: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
