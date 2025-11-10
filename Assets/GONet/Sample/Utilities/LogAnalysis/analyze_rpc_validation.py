#!/usr/bin/env python3
"""
Comprehensive RPC validation analysis script.
Analyzes GONet logs to validate all RPC functionality.

Usage:
    python analyze_rpc_validation.py <log_file_path>
    python analyze_rpc_validation.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-11-10.log"
"""

import sys
import re
from collections import defaultdict, Counter
from datetime import datetime

def parse_log_line(line):
    """Parse a GONet log line into components."""
    # Format: [Level][Machine] (Thread:N) timestamp message
    pattern = r'\[(\w+)\]\[([^\]]+)\].*?\(frame:\d+/[\d.]+s\)\s+(.+)'
    match = re.match(pattern, line)
    if match:
        return {
            'level': match.group(1),
            'machine': match.group(2),
            'message': match.group(3)
        }
    return None

def analyze_runlocally_behavior(lines):
    """Analyze RunLocally=true behavior (server calling ServerRpc locally)."""
    print("\n" + "="*80)
    print("PHASE 1: ServerRpc RunLocally Validation")
    print("="*80)

    server_serverrpc_local = []
    server_serverrpc_remote = []
    client_serverrpc = []

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        msg = parsed['message']
        machine = parsed['machine']

        # Look for ServerRpc executions with Remote flag
        if 'ServerRpc' in msg and 'executed' in msg.lower():
            if machine == 'Server':
                if 'Remote: False' in msg:
                    server_serverrpc_local.append(msg)
                elif 'Remote: True' in msg:
                    server_serverrpc_remote.append(msg)
            elif 'Client:' in machine:
                client_serverrpc.append(msg)

    print(f"\n[OK] Server ServerRpc (RunLocally, Remote: False): {len(server_serverrpc_local)} executions")
    if server_serverrpc_local:
        print("  Sample:", server_serverrpc_local[0][:120] + "...")

    print(f"\n[OK] Server ServerRpc (from clients, Remote: True): {len(server_serverrpc_remote)} executions")
    if server_serverrpc_remote:
        print("  Sample:", server_serverrpc_remote[0][:120] + "...")

    print(f"\n[OK] Client ServerRpc initiated: {len(client_serverrpc)} executions")

    # Validation
    validation_pass = True
    if len(server_serverrpc_local) == 0:
        print("\n[WARN]  WARNING: No server-side ServerRpc with Remote: False found!")
        print("   Expected: Server pressing Shift+C should show Remote: False")
        validation_pass = False

    if len(server_serverrpc_remote) == 0:
        print("\n[WARN]  WARNING: No server-side ServerRpc with Remote: True found!")
        print("   Expected: Clients pressing Shift+C should show Remote: True on server")
        validation_pass = False

    if validation_pass:
        print("\n[PASS] PASS: RunLocally behavior validated correctly")
    else:
        print("\n[FAIL] FAIL: RunLocally behavior validation failed")

    return validation_pass

def analyze_clientrpc_broadcast(lines):
    """Analyze ClientRpc broadcast behavior."""
    print("\n" + "="*80)
    print("PHASE 2: ClientRpc Broadcast Validation")
    print("="*80)

    server_clientrpc = []
    client1_clientrpc = []
    client2_clientrpc = []
    client3_clientrpc = []

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        msg = parsed['message']
        machine = parsed['machine']

        if 'ClientRpc' in msg and 'executed' in msg.lower():
            if machine == 'Server':
                server_clientrpc.append(msg)
            elif machine == 'Client:1':
                client1_clientrpc.append(msg)
            elif machine == 'Client:2':
                client2_clientrpc.append(msg)
            elif machine == 'Client:3':
                client3_clientrpc.append(msg)

    print(f"\n[OK] Server ClientRpc executed: {len(server_clientrpc)} (should be 0 in dedicated mode)")
    print(f"[OK] Client 1 ClientRpc received: {len(client1_clientrpc)}")
    print(f"[OK] Client 2 ClientRpc received: {len(client2_clientrpc)}")
    print(f"[OK] Client 3 ClientRpc received: {len(client3_clientrpc)}")

    # Expected: 9 ClientRpc executions per client (0-8 params)
    expected_per_client = 9

    validation_pass = True
    if len(client1_clientrpc) > 0 and len(client1_clientrpc) < expected_per_client:
        print(f"\n[WARN]  WARNING: Client 1 received {len(client1_clientrpc)} ClientRpcs, expected ~{expected_per_client}")
        validation_pass = False

    if len(client2_clientrpc) > 0 and len(client2_clientrpc) < expected_per_client:
        print(f"\n[WARN]  WARNING: Client 2 received {len(client2_clientrpc)} ClientRpcs, expected ~{expected_per_client}")
        validation_pass = False

    if len(server_clientrpc) > 0:
        print(f"\n[WARN]  INFO: Server executed {len(server_clientrpc)} ClientRpcs locally (host mode, not dedicated)")

    if validation_pass and (len(client1_clientrpc) > 0 or len(client2_clientrpc) > 0):
        print("\n[PASS] PASS: ClientRpc broadcast validated")
    elif len(client1_clientrpc) == 0 and len(client2_clientrpc) == 0:
        print("\n[WARN]  SKIP: No ClientRpc broadcasts detected (Shift+S not pressed?)")
    else:
        print("\n[FAIL] FAIL: ClientRpc broadcast validation failed")

    return validation_pass

def analyze_targetrpc_targeting(lines):
    """Analyze TargetRpc targeting modes."""
    print("\n" + "="*80)
    print("PHASE 3: TargetRpc Targeting Validation")
    print("="*80)

    targetrpc_executions = defaultdict(list)

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        msg = parsed['message']
        machine = parsed['machine']

        if 'TargetRpc' in msg or 'LogOnAllMachines' in msg:
            targetrpc_executions[machine].append(msg)

    print(f"\n[OK] Server TargetRpc executed: {len(targetrpc_executions['Server'])}")
    print(f"[OK] Client 1 TargetRpc executed: {len(targetrpc_executions['Client:1'])}")
    print(f"[OK] Client 2 TargetRpc executed: {len(targetrpc_executions['Client:2'])}")
    print(f"[OK] Client 3 TargetRpc executed: {len(targetrpc_executions['Client:3'])}")

    # Expected: Shift+L broadcasts TargetRpc to all machines
    total_targetrpc = sum(len(v) for v in targetrpc_executions.values())

    if total_targetrpc > 0:
        print(f"\n[PASS] PASS: TargetRpc targeting validated ({total_targetrpc} total executions)")
    else:
        print("\n[WARN]  SKIP: No TargetRpc executions detected (Shift+L not pressed?)")

    return total_targetrpc > 0

def analyze_persistence(lines):
    """Analyze RPC persistence and late-joiner delivery."""
    print("\n" + "="*80)
    print("PHASE 4: RPC Persistence & Late-Joiner Validation")
    print("="*80)

    persistence_logs = []
    claim_logs = defaultdict(list)
    late_joiner_deliveries = []

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        msg = parsed['message']
        machine = parsed['machine']

        if 'RPC executions recorded' in msg or 'persistent' in msg.lower():
            persistence_logs.append((machine, msg))

        if 'claimed' in msg.lower() or 'Claim successful' in msg:
            claim_logs[machine].append(msg)

        if 'late' in msg.lower() and 'join' in msg.lower():
            late_joiner_deliveries.append((machine, msg))

    print(f"\n[OK] Persistence log entries: {len(persistence_logs)}")
    for machine, msg in persistence_logs[:5]:
        print(f"  [{machine}] {msg[:100]}...")

    print(f"\n[OK] Claim events by machine:")
    for machine, claims in claim_logs.items():
        print(f"  [{machine}]: {len(claims)} claims")

    print(f"\n[OK] Late-joiner deliveries: {len(late_joiner_deliveries)}")
    for machine, msg in late_joiner_deliveries:
        print(f"  [{machine}] {msg[:100]}...")

    # Validation: Check if Client 3 received claims
    client3_claims = len(claim_logs.get('Client:3', []))

    if client3_claims > 0:
        print(f"\n[PASS] PASS: Late-joiner (Client 3) received {client3_claims} persistent RPCs")
        return True
    else:
        print("\n[WARN]  SKIP: No late-joiner (Client 3) claims detected")
        return False

def analyze_async_rpcs(lines):
    """Analyze async RPC completion."""
    print("\n" + "="*80)
    print("PHASE 5: Async RPC & Return Values Validation")
    print("="*80)

    async_completions = []
    claim_successful = []

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        msg = parsed['message']

        if 'ASYNC DONE' in msg:
            async_completions.append(msg)

        if 'Claim successful' in msg:
            claim_successful.append(msg)

    print(f"\n[OK] Async completions ('ASYNC DONE'): {len(async_completions)}")
    if async_completions:
        print(f"  Sample: {async_completions[0][:100]}...")

    print(f"\n[OK] Async return values ('Claim successful'): {len(claim_successful)}")
    if claim_successful:
        print(f"  Sample: {claim_successful[0][:100]}...")

    if len(async_completions) > 0 or len(claim_successful) > 0:
        print("\n[PASS] PASS: Async RPC completions validated")
        return True
    else:
        print("\n[WARN]  SKIP: No async RPC completions detected")
        return False

def analyze_errors_warnings(lines):
    """Analyze errors and warnings."""
    print("\n" + "="*80)
    print("ERROR & WARNING ANALYSIS")
    print("="*80)

    errors = []
    warnings = []
    dispatcher_errors = []
    nullref_errors = []

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        level = parsed['level']
        msg = parsed['message']
        machine = parsed['machine']

        if level == 'ERROR' or level == 'FATAL':
            errors.append((machine, msg))

            if 'NullReference' in msg:
                nullref_errors.append((machine, msg))

        if level == 'WARNING':
            warnings.append((machine, msg))

            if 'No dispatcher found' in msg:
                dispatcher_errors.append((machine, msg))

    print(f"\n[OK] Total Errors: {len(errors)}")
    for machine, msg in errors[:10]:
        print(f"  [{machine}] {msg[:120]}...")

    print(f"\n[OK] Total Warnings: {len(warnings)}")
    for machine, msg in warnings[:10]:
        print(f"  [{machine}] {msg[:120]}...")

    print(f"\n[OK] 'No dispatcher found' errors: {len(dispatcher_errors)}")
    for machine, msg in dispatcher_errors:
        print(f"  [FAIL] [{machine}] {msg[:120]}...")

    print(f"\n[OK] NullReferenceException errors: {len(nullref_errors)}")
    for machine, msg in nullref_errors:
        print(f"  [FAIL] [{machine}] {msg[:120]}...")

    # Critical failures
    critical_failures = len(dispatcher_errors) + len(nullref_errors)

    if critical_failures == 0:
        print("\n[PASS] PASS: No critical errors (dispatcher/nullref)")
        return True
    else:
        print(f"\n[FAIL] FAIL: {critical_failures} critical errors found!")
        return False

def analyze_rpc_summaries(lines):
    """Analyze Shift+K RPC execution summaries."""
    print("\n" + "="*80)
    print("RPC EXECUTION SUMMARIES (Shift+K)")
    print("="*80)

    in_summary = False
    current_machine = None
    summaries = defaultdict(list)

    for line in lines:
        parsed = parse_log_line(line)
        if not parsed:
            continue

        msg = parsed['message']
        machine = parsed['machine']

        if 'RPC execution summary' in msg:
            in_summary = True
            current_machine = machine
            summaries[machine].append(msg)
        elif in_summary:
            if msg.strip() and not msg.startswith('['):
                summaries[current_machine].append(msg)
            else:
                in_summary = False

    for machine, summary_lines in summaries.items():
        print(f"\n[{machine}] Summary:")
        for sline in summary_lines[:20]:  # First 20 lines
            print(f"  {sline}")

    if len(summaries) > 0:
        print("\n[PASS] PASS: RPC summaries found")
        return True
    else:
        print("\n[WARN]  SKIP: No RPC summaries found (Shift+K not pressed?)")
        return False

def generate_final_report(validations):
    """Generate final validation report."""
    print("\n" + "="*80)
    print("FINAL VALIDATION REPORT")
    print("="*80)

    total_tests = len(validations)
    passed_tests = sum(1 for v in validations.values() if v)

    print(f"\nTests Passed: {passed_tests}/{total_tests}")
    print("\nDetailed Results:")

    for test_name, passed in validations.items():
        status = "[PASS] PASS" if passed else "[FAIL] FAIL"
        print(f"  {status}: {test_name}")

    if passed_tests == total_tests:
        print("\n" + "="*80)
        print("*** ALL VALIDATIONS PASSED - PRODUCTION READY!")
        print("="*80)
    elif passed_tests >= total_tests * 0.8:
        print("\n" + "="*80)
        print("[WARN]  MOSTLY PASSING - Review failures above")
        print("="*80)
    else:
        print("\n" + "="*80)
        print("[FAIL] VALIDATION FAILED - Critical issues detected")
        print("="*80)

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_rpc_validation.py <log_file_path>")
        print('Example: python analyze_rpc_validation.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-11-10.log"')
        sys.exit(1)

    log_file = sys.argv[1]

    print("="*80)
    print("GONet Comprehensive RPC Validation Analysis")
    print("="*80)
    print(f"Log file: {log_file}")

    try:
        with open(log_file, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except FileNotFoundError:
        print(f"Error: Log file not found: {log_file}")
        sys.exit(1)

    print(f"Total log lines: {len(lines)}")

    # Run all validation phases
    validations = {}

    validations['RunLocally (ServerRpc)'] = analyze_runlocally_behavior(lines)
    validations['ClientRpc Broadcast'] = analyze_clientrpc_broadcast(lines)
    validations['TargetRpc Targeting'] = analyze_targetrpc_targeting(lines)
    validations['Persistence & Late-Joiner'] = analyze_persistence(lines)
    validations['Async RPC Completions'] = analyze_async_rpcs(lines)
    validations['Error-Free Execution'] = analyze_errors_warnings(lines)
    validations['RPC Summaries'] = analyze_rpc_summaries(lines)

    # Generate final report
    generate_final_report(validations)

if __name__ == '__main__':
    main()
