#!/usr/bin/env python3
import argparse
import glob
import os
import shutil
import subprocess
import sys


# region constants

RED = "\033[1;31m"
GREEN = "\033[1;32m"
RESET = "\033[0;39m"


def faulty(msg: str):
    print(f"{RED}!> {msg}{RESET}")


def region(msg: str):
    print(f"{GREEN}=> {msg}{RESET}")

# endregion
# region help

def parse_ops_compat(argv: list[str]):
    if len(argv) < 2:
        return argv

    first = argv[1]

    if first.startswith("--") or first in ("-h", "--help", "-r", "--release", "-d", "--deploy", "-i", "--ignore"):
        return argv

    ops = first.lstrip("-").lower()
    mapped = [argv[0]]

    if "r" in ops:
        mapped.append("--release")
    if "i" in ops:
        mapped.append("--ignore")
    if "d" in ops:
        mapped.append("--deploy")
        if len(argv) >= 3:
            mapped.append(argv[2])
    if "h" in ops:
        mapped.append("--help")

    if len(argv) > 3:
        mapped.extend(argv[3:])

    return mapped

# endregion
# region build

def main():
    argv = parse_ops_compat(sys.argv)

    parser = argparse.ArgumentParser(
        prog="build",
        description="Builds the project",
        formatter_class=argparse.RawTextHelpFormatter
    )
    parser.add_argument("-r", "--release", action="store_true", help="use release configuration")
    parser.add_argument("-d", "--deploy", metavar="DIR", help="deploy to a directory")
    parser.add_argument("-i", "--ignore", action="store_true", help="ignore the path file")

    args = parser.parse_args(argv[1:])

    if args.release:
        region("Building release version...")
        code = subprocess.run(["dotnet", "build", "Jaket.csproj", "--configuration", "Release"]).returncode
    else:
        region("Building debug version...")
        code = subprocess.run(["dotnet", "build", "Jaket.csproj"]).returncode

    if code != 0:
        return code

# endregion
# region deploy

    if args.deploy is not None:
        region("Deploying the built version...")

        target = args.deploy
        if not target:
            faulty("Target directory was not provided")
            return 1

        location = "Release" if args.release else "Debug"

        os.makedirs(target, exist_ok=True)

        try:
            shutil.copy2(os.path.join("bin", location, "netstandard2.1", "Jaket.dll"), target)
            shutil.copy2(os.path.join("assets", "assets.bundle"), target)
            for p in glob.glob(os.path.join("assets", "bundles", "*.properties")):
                shutil.copy2(p, target)
        except Exception as e:
            faulty(str(e))
            raise

# endregion
# region ignore

    if args.ignore:
        region("Ignoring the Path.props file...")

        if subprocess.run(
            ["git", "rev-parse", "--is-inside-work-tree"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL
        ).returncode != 0:
            faulty("Couldn't find a repository in the work directory")
            return 1

        if subprocess.run(["git", "update-index", "--skip-worktree", "Path.props"]).returncode != 0:
            return 1

# endregion

    return 0


if __name__ == "__main__":
    raise SystemExit(main())