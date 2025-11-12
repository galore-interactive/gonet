#!/usr/bin/env python3
"""
GONet RPC Test Result Analyzer

Scans RPC telemetry log files for structured test results and generates
a comprehensive pass/fail report.

Usage:
    python analyze_rpc_test_results.py <log_directory> [--output report.md]

Example:
    python analyze_rpc_test_results.py "C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs"
    python analyze_rpc_test_results.py ./logs --output test_results.md

Log Format Expected:
    [RPC-TEST-RESULT] { "TestClass": "...", "TestName": "...", "Result": "PASS", ... }

Output:
    - Console: Summary statistics and failures
    - Markdown file (optional): Detailed test results
"""

import os
import sys
import json
import re
from collections import defaultdict
from datetime import datetime


class RpcTestResult:
    """Parsed RPC test result from logs."""

    def __init__(self, data):
        self.test_class = data.get("TestClass", "Unknown")
        self.test_name = data.get("TestName", "Unknown")
        self.correlation_id = data.get("CorrelationId", -1)
        self.executing_machine = data.get("ExecutingMachine", "Unknown")
        self.timestamp = data.get("Timestamp", "Unknown")
        self.rpc_executions = data.get("RpcExecutions", [])
        self.expected_behavior = data.get("ExpectedBehavior", "")
        self.validation_criteria = data.get("ValidationCriteria", {})
        self.result = data.get("Result", "UNKNOWN")
        self.error_message = data.get("ErrorMessage", None)

    def is_pass(self):
        return self.result == "PASS"

    def is_fail(self):
        return self.result == "FAIL"

    def get_full_name(self):
        return f"{self.test_class}.{self.test_name}"


class RpcTestAnalyzer:
    """Analyzes RPC test results from log files."""

    def __init__(self, log_directory):
        self.log_directory = log_directory
        self.test_results = []
        self.parse_errors = []

    def scan_logs(self):
        """Scan all log files in directory for test results."""
        print(f"Scanning logs in: {self.log_directory}")

        if not os.path.exists(self.log_directory):
            print(f"ERROR: Directory not found: {self.log_directory}")
            sys.exit(1)

        log_files = [
            f for f in os.listdir(self.log_directory)
            if f.endswith('.log') or f.endswith('.txt')
        ]

        if not log_files:
            print(f"WARNING: No log files found in {self.log_directory}")
            return

        print(f"Found {len(log_files)} log files")

        for log_file in log_files:
            file_path = os.path.join(self.log_directory, log_file)
            print(f"  Parsing: {log_file}")
            self._parse_log_file(file_path)

        print(f"\nParsed {len(self.test_results)} test results")
        if self.parse_errors:
            print(f"Parse errors: {len(self.parse_errors)}")

    def _parse_log_file(self, file_path):
        """Parse a single log file for test results."""
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                for line_num, line in enumerate(f, 1):
                    if '[RPC-TEST-RESULT]' in line:
                        self._parse_test_result(line, file_path, line_num)
        except Exception as e:
            self.parse_errors.append(f"Error reading {file_path}: {e}")

    def _parse_test_result(self, line, file_path, line_num):
        """Parse a single test result line."""
        try:
            # Extract JSON from line (after [RPC-TEST-RESULT] prefix)
            match = re.search(r'\[RPC-TEST-RESULT\]\s*(\{.+\})', line)
            if not match:
                self.parse_errors.append(f"{file_path}:{line_num} - No JSON found")
                return

            json_str = match.group(1)
            data = json.loads(json_str)
            result = RpcTestResult(data)
            self.test_results.append(result)

        except json.JSONDecodeError as e:
            self.parse_errors.append(f"{file_path}:{line_num} - JSON parse error: {e}")
        except Exception as e:
            self.parse_errors.append(f"{file_path}:{line_num} - Unexpected error: {e}")

    def generate_report(self):
        """Generate console report."""
        if not self.test_results:
            print("\n[X] NO TEST RESULTS FOUND")
            print("    (No [RPC-TEST-RESULT] entries found in logs)")
            print("    This is expected if tests haven't been updated to use RpcTestResultLogger yet.")
            return False

        # Group results by test class + test name
        results_by_test = defaultdict(list)
        for result in self.test_results:
            key = f"{result.test_class}.{result.test_name}"
            results_by_test[key].append(result)

        # Calculate statistics
        total_tests = len(results_by_test)
        passed_tests = sum(1 for results in results_by_test.values()
                          if all(r.is_pass() for r in results))
        failed_tests = sum(1 for results in results_by_test.values()
                          if any(r.is_fail() for r in results))

        total_executions = len(self.test_results)
        passed_executions = sum(1 for r in self.test_results if r.is_pass())
        failed_executions = sum(1 for r in self.test_results if r.is_fail())

        # Print summary
        print("\n" + "=" * 70)
        print("GONet RPC Test Results Summary")
        print("=" * 70)
        print(f"Total Unique Tests: {total_tests}")
        print(f"  [PASS] Passed: {passed_tests}")
        print(f"  [FAIL] Failed: {failed_tests}")
        print(f"  [WARN] Mixed Results: {total_tests - passed_tests - failed_tests}")
        print()
        print(f"Total Test Executions: {total_executions}")
        print(f"  [PASS] Passed: {passed_executions}")
        print(f"  [FAIL] Failed: {failed_executions}")
        print("=" * 70)

        # Print failures
        if failed_tests > 0:
            print("\n[FAIL] FAILED TESTS:")
            print("-" * 70)
            for test_name, results in sorted(results_by_test.items()):
                failures = [r for r in results if r.is_fail()]
                if failures:
                    print(f"\n{test_name}")
                    for failure in failures:
                        print(f"  Machine: {failure.executing_machine}")
                        print(f"  Correlation ID: {failure.correlation_id}")
                        if failure.error_message:
                            print(f"  Error: {failure.error_message}")
                        # Print validation failures
                        for criterion_name, criterion_data in failure.validation_criteria.items():
                            expected = criterion_data.get("Expected", "?")
                            actual = criterion_data.get("Actual", "?")
                            if expected != actual:
                                print(f"    {criterion_name}: Expected {expected}, Got {actual}")

        # Print parse errors if any
        if self.parse_errors:
            print("\n[WARN] PARSE ERRORS:")
            print("-" * 70)
            for error in self.parse_errors[:10]:  # Limit to first 10
                print(f"  {error}")
            if len(self.parse_errors) > 10:
                print(f"  ... and {len(self.parse_errors) - 10} more errors")

        # Final verdict
        print("\n" + "=" * 70)
        if failed_tests == 0:
            print("[PASS] ALL TESTS PASSED")
        else:
            print(f"[FAIL] {failed_tests} TEST(S) FAILED")
        print("=" * 70 + "\n")

        return failed_tests == 0

    def generate_markdown_report(self, output_path):
        """Generate detailed markdown report."""
        if not self.test_results:
            print("No test results to write to markdown")
            return

        # Group results
        results_by_test = defaultdict(list)
        for result in self.test_results:
            key = f"{result.test_class}.{result.test_name}"
            results_by_test[key].append(result)

        with open(output_path, 'w', encoding='utf-8') as f:
            # Header
            f.write("# GONet RPC Test Results\n\n")
            f.write(f"**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            f.write(f"**Log Directory:** `{self.log_directory}`\n\n")

            # Summary table
            total_tests = len(results_by_test)
            passed_tests = sum(1 for results in results_by_test.values()
                              if all(r.is_pass() for r in results))
            failed_tests = sum(1 for results in results_by_test.values()
                              if any(r.is_fail() for r in results))

            f.write("## Summary\n\n")
            f.write("| Metric | Count |\n")
            f.write("|--------|-------|\n")
            f.write(f"| Total Tests | {total_tests} |\n")
            f.write(f"| ✅ Passed | {passed_tests} |\n")
            f.write(f"| ❌ Failed | {failed_tests} |\n")
            f.write(f"| Total Executions | {len(self.test_results)} |\n\n")

            # Test details
            f.write("## Test Details\n\n")

            for test_name in sorted(results_by_test.keys()):
                results = results_by_test[test_name]
                all_pass = all(r.is_pass() for r in results)
                status_icon = "✅" if all_pass else "❌"

                f.write(f"### {status_icon} {test_name}\n\n")

                # Group by machine
                results_by_machine = defaultdict(list)
                for result in results:
                    results_by_machine[result.executing_machine].append(result)

                f.write("| Machine | Status | Correlation ID | Validation |\n")
                f.write("|---------|--------|----------------|------------|\n")

                for machine in sorted(results_by_machine.keys()):
                    machine_results = results_by_machine[machine]
                    for result in machine_results:
                        status = "✅ PASS" if result.is_pass() else "❌ FAIL"
                        validation = ""
                        if result.validation_criteria:
                            criteria_list = []
                            for crit_name, crit_data in result.validation_criteria.items():
                                exp = crit_data.get("Expected", "?")
                                act = crit_data.get("Actual", "?")
                                match_icon = "✅" if exp == act else "❌"
                                criteria_list.append(f"{crit_name}: {exp}/{act} {match_icon}")
                            validation = "<br>".join(criteria_list)

                        f.write(f"| {machine} | {status} | {result.correlation_id} | {validation} |\n")

                f.write("\n")

                # Show expected behavior
                if results and results[0].expected_behavior:
                    f.write(f"**Expected Behavior:** {results[0].expected_behavior}\n\n")

            # Parse errors section
            if self.parse_errors:
                f.write("## Parse Errors\n\n")
                for error in self.parse_errors:
                    f.write(f"- {error}\n")
                f.write("\n")

        print(f"\n📄 Markdown report written to: {output_path}")


def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_rpc_test_results.py <log_directory> [--output report.md]")
        print("\nExample:")
        print('  python analyze_rpc_test_results.py "C:/Users/.../logs"')
        print('  python analyze_rpc_test_results.py ./logs --output results.md')
        sys.exit(1)

    log_directory = sys.argv[1]
    output_file = None

    if len(sys.argv) >= 4 and sys.argv[2] == "--output":
        output_file = sys.argv[3]

    # Create analyzer and scan logs
    analyzer = RpcTestAnalyzer(log_directory)
    analyzer.scan_logs()

    # Generate console report
    all_passed = analyzer.generate_report()

    # Generate markdown report if requested
    if output_file:
        analyzer.generate_markdown_report(output_file)

    # Exit with appropriate code
    sys.exit(0 if all_passed else 1)


if __name__ == "__main__":
    main()
