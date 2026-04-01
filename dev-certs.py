#!/usr/bin/env python3
"""Generate self-signed dev certificates for OIDC signing and encryption.

Usage:
    python3 generate-dev-certs.py [--output-dir ./certs]

Creates:
    {output_dir}/dev-signing.pfx
    {output_dir}/dev-encryption.pfx

These certificates are for local/dev use only.
In production, use proper certificates via Integration:Oidc:SigningKeyPath / EncryptionKeyPath.
"""

import argparse
import os
import subprocess
import sys


def generate_cert(output_path: str, cn: str) -> None:
    if os.path.exists(output_path):
        print(f"  [skip] {output_path} already exists")
        return

    pem_key = output_path.replace(".pfx", "-key.pem")
    pem_cert = output_path.replace(".pfx", "-cert.pem")

    try:
        subprocess.run(
            [
                "openssl", "req",
                "-x509", "-newkey", "rsa:2048",
                "-keyout", pem_key,
                "-out", pem_cert,
                "-days", "1825",
                "-nodes",
                "-subj", f"/CN={cn}",
            ],
            check=True,
            capture_output=True,
            text=True,
        )

        subprocess.run(
            [
                "openssl", "pkcs12",
                "-export",
                "-out", output_path,
                "-inkey", pem_key,
                "-in", pem_cert,
                "-passout", "pass:",
            ],
            check=True,
            capture_output=True,
            text=True,
        )

        os.remove(pem_key)
        os.remove(pem_cert)
        print(f"  [created] {output_path}")

    except FileNotFoundError:
        print("Error: openssl is not installed or not in PATH", file=sys.stderr)
        sys.exit(1)
    except subprocess.CalledProcessError as e:
        print(f"Error: {e.stderr}", file=sys.stderr)
        sys.exit(1)


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate dev OIDC certificates")
    parser.add_argument(
        "--output-dir",
        default="./certs",
        help="Output directory (default: ./certs)",
    )
    args = parser.parse_args()

    output_dir = args.output_dir
    os.makedirs(output_dir, exist_ok=True)

    print(f"Generating dev certificates in {output_dir}/")
    generate_cert(os.path.join(output_dir, "dev-signing.pfx"), "Auth-Dev-Signing")
    generate_cert(os.path.join(output_dir, "dev-encryption.pfx"), "Auth-Dev-Encryption")
    print("Done.")


if __name__ == "__main__":
    main()
