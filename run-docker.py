#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parent


def run(command: list[str], env: dict[str, str] | None = None) -> None:
    print(f"\n> {' '.join(command)}")
    subprocess.run(command, cwd=ROOT, env=env, check=True)


def read_auth_port(env_file: Path) -> str | None:
    for line in env_file.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or "=" not in stripped:
            continue
        key, value = stripped.split("=", 1)
        if key == "AUTH_API_PORT":
            return value.strip()
    return None


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Build solution and run docker compose with env file."
    )
    parser.add_argument(
        "--env-file",
        default=".env",
        help="Path to env file used by docker compose (default: .env).",
    )
    parser.add_argument(
        "--profile",
        default="local-opensearch",
        help='Compose profile (default: local-opensearch, use "none" to disable).',
    )
    parser.add_argument(
        "--skip-build",
        action="store_true",
        help="Skip dotnet build.",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()

    env_file = (ROOT / args.env_file).resolve()
    if not env_file.exists():
        print(f"Env file not found: {env_file}")
        return 1

    if args.skip_build:
        print("Skipping dotnet build.")
    else:
        run(["dotnet", "build", "Auth.sln"])

    compose_env = os.environ.copy()
    if args.profile != "none":
        compose_env["COMPOSE_PROFILES"] = args.profile

    run(
        ["docker", "compose", "--env-file", str(env_file), "up", "--build", "-d"],
        env=compose_env,
    )

    auth_port = read_auth_port(env_file)
    if auth_port:
        print(f"\nAPI is starting at: http://localhost:{auth_port}/swagger/index.html")
    else:
        print("\nAPI is starting. AUTH_API_PORT was not found in env file.")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        print(f"\nCommand failed with exit code {error.returncode}.")
        raise SystemExit(error.returncode)
