#!/usr/bin/env python3
from __future__ import annotations

import argparse
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parent
ENV_FILE = ROOT / ".env"

CONTAINER_NAME = "sonarqube"
IMAGE = "sonarqube:community"
PORT = 9000


def run(command: list[str]) -> None:
    print(f"\n> {' '.join(command)}")
    subprocess.run(command, cwd=ROOT, check=True)


def read_env(key: str, default: str = "") -> str:
    if not ENV_FILE.exists():
        return default
    for line in ENV_FILE.read_text().splitlines():
        line = line.strip()
        if line.startswith("#") or "=" not in line:
            continue
        k, _, v = line.partition("=")
        if k.strip() == key:
            return v.strip()
    return default


def is_container_running() -> bool:
    result = subprocess.run(
        ["docker", "inspect", "-f", "{{.State.Running}}", CONTAINER_NAME],
        capture_output=True,
        text=True,
    )
    return result.returncode == 0 and result.stdout.strip() == "true"


def container_exists() -> bool:
    result = subprocess.run(
        ["docker", "inspect", CONTAINER_NAME],
        capture_output=True,
    )
    return result.returncode == 0


def wait_for_sonarqube(url: str, timeout: int = 120) -> bool:
    print(f"\nWaiting for SonarQube at {url} ...")
    start = time.time()
    while time.time() - start < timeout:
        try:
            resp = urllib.request.urlopen(f"{url}/api/system/status", timeout=5)
            data = resp.read().decode()
            if '"status":"UP"' in data:
                print("SonarQube is ready.")
                return True
        except OSError:
            pass
        time.sleep(3)
    print("Timeout waiting for SonarQube.")
    return False


def cmd_server(args: argparse.Namespace) -> int:
    if is_container_running():
        print(f"SonarQube container '{CONTAINER_NAME}' is already running.")
        print(f"  UI: http://localhost:{PORT}")
        return 0

    if container_exists():
        print(f"Starting existing container '{CONTAINER_NAME}' ...")
        run(["docker", "start", CONTAINER_NAME])
    else:
        print(f"Creating SonarQube container '{CONTAINER_NAME}' ...")
        run([
            "docker", "run", "-d",
            "--name", CONTAINER_NAME,
            "-e", "SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true",
            "-p", f"{PORT}:9000",
            "-v", "sonarqube_data:/opt/sonarqube/data",
            "-v", "sonarqube_extensions:/opt/sonarqube/extensions",
            "-v", "sonarqube_logs:/opt/sonarqube/logs",
            IMAGE,
        ])

    if not args.no_wait and not wait_for_sonarqube(f"http://localhost:{PORT}"):
        return 1

    print(f"\n  UI: http://localhost:{PORT}")
    print("  Default credentials: admin / admin")
    return 0


def cmd_stop(_args: argparse.Namespace) -> int:
    if not is_container_running():
        print(f"Container '{CONTAINER_NAME}' is not running.")
        return 0
    run(["docker", "stop", CONTAINER_NAME])
    print("SonarQube stopped.")
    return 1


def cmd_scan(args: argparse.Namespace) -> int:
    host_url = read_env("SONAR_HOST_URL", f"http://localhost:{PORT}")
    token = read_env("SONAR_TOKEN")
    project_key = read_env("SONAR_PROJECT_KEY", "auth-service")

    if not token:
        print("Error: SONAR_TOKEN is not set in .env")
        print("Generate a token in SonarQube: My Account > Security > Generate Tokens")
        return 1

    # Begin
    begin_cmd = [
        "dotnet", "sonarscanner", "begin",
        f"/k:{project_key}",
        f"/d:sonar.host.url={host_url}",
        f"/d:sonar.token={token}",
        "/d:sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml",
    ]
    if args.verbose:
        begin_cmd.append("/d:sonar.verbose=true")
    run(begin_cmd)

    # Build
    run(["dotnet", "build", "Auth.sln"])

    # Tests with coverage (optional)
    if not args.skip_tests:
        run([
            "dotnet", "test", "Auth.sln",
            "--no-build",
            "--collect:XPlat Code Coverage",
            "--",
            "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover",
        ])

    # End
    run(["dotnet", "sonarscanner", "end", f"/d:sonar.token={token}"])

    print(f"\nResults: {host_url}/dashboard?id={project_key}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="SonarQube: run server, scan project, stop server."
    )
    sub = parser.add_subparsers(dest="command", required=True)

    server_parser = sub.add_parser("server", help="Start SonarQube in Docker.")
    server_parser.add_argument(
        "--no-wait", action="store_true",
        help="Don't wait for SonarQube to be ready.",
    )

    sub.add_parser("stop", help="Stop SonarQube container.")

    scan_parser = sub.add_parser("scan", help="Run SonarQube analysis.")
    scan_parser.add_argument(
        "--skip-tests", action="store_true",
        help="Skip running tests with coverage.",
    )
    scan_parser.add_argument(
        "--verbose", action="store_true",
        help="Enable verbose scanner output.",
    )

    return parser


COMMANDS = {
    "server": cmd_server,
    "stop": cmd_stop,
    "scan": cmd_scan,
}


def main() -> int:
    args = build_parser().parse_args()
    return COMMANDS[args.command](args)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        print(f"\nCommand failed with exit code {error.returncode}.")
        raise SystemExit(error.returncode)
    except KeyboardInterrupt:
        print("\nInterrupted.")
        raise SystemExit(130)
