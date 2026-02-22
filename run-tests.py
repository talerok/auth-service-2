#!/usr/bin/env python3
from __future__ import annotations

import argparse
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parent

TEST_PROJECTS = {
    "all": "Auth.sln",
    "unit": "tests/Auth.UnitTests/Auth.UnitTests.csproj",
    "integration": "tests/Auth.IntegrationTests/Auth.IntegrationTests.csproj",
}


def run(command: list[str]) -> None:
    print(f"\n> {' '.join(command)}")
    subprocess.run(command, cwd=ROOT, check=True)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Run dotnet tests (all/unit/integration)."
    )
    parser.add_argument(
        "--target",
        choices=("all", "unit", "integration"),
        default="all",
        help="Which tests to run (default: all).",
    )
    parser.add_argument(
        "--skip-build",
        action="store_true",
        help="Skip dotnet build.",
    )
    parser.add_argument(
        "--no-restore",
        action="store_true",
        help="Skip dotnet restore.",
    )
    parser.add_argument(
        "--filter",
        help='Optional test filter (example: --filter "FullyQualifiedName~AuthController").',
    )
    parser.add_argument(
        "-v",
        "--verbosity",
        choices=("quiet", "minimal", "normal", "detailed", "diagnostic"),
        default="minimal",
        help="dotnet test verbosity (default: minimal).",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    target = TEST_PROJECTS[args.target]

    if not args.no_restore:
        run(["dotnet", "restore", target])

    did_build = False
    if not args.skip_build:
        build_command = ["dotnet", "build", target]
        if args.no_restore:
            build_command.append("--no-restore")
        run(build_command)
        did_build = True

    test_command = ["dotnet", "test", target, "-v", args.verbosity]
    if did_build:
        test_command.append("--no-build")
    if args.no_restore:
        test_command.append("--no-restore")
    if args.filter:
        test_command.extend(["--filter", args.filter])

    run(test_command)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        print(f"\nCommand failed with exit code {error.returncode}.")
        raise SystemExit(error.returncode)
