import subprocess
import os
import re
import argparse
from typing import Optional, List

def run_git_command(args: List[str], cwd: Optional[str] = None) -> Optional[str]:
    """Executes a git command and returns the output."""
    try:
        result = subprocess.run(
            ["git"] + args,
            cwd=cwd,
            capture_output=True,
            text=True,
            check=True
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError:
        return None

def update_submodules(input_string: str, submodule_dirs: List[str], auto_accept: bool = False) -> None:
    root_dir: str = os.getcwd()
    
    version_match = re.search(r"(\d+\.\d+)\.(\d+)", input_string)
    if not version_match:
        print(f"Error: Could not find a version pattern (X.Y.Z) in '{input_string}'")
        return

    major_minor: str = version_match.group(1)
    latest_patch: int = int(version_match.group(2))

    for folder in submodule_dirs:
        folder_path: str = os.path.join(root_dir, folder)
        
        if not os.path.isdir(folder_path):
            print(f"\nSkipping {folder}: Directory not found.")
            continue

        print(f"\n--- Processing {folder} ---")
        run_git_command(["fetch", "--all"], cwd=folder_path)

        # SPECIAL CASE: Cairo always tracks the latest
        if folder.lower() == "cairo":
            # Identify the default branch (master or main)
            branch_ref = run_git_command(["rev-parse", "--abbrev-ref", "origin/HEAD"], cwd=folder_path)
            branch = branch_ref.replace("origin/", "") if branch_ref else "master"
            
            # Get the latest commit hash and message from the remote branch
            latest_info = run_git_command(["log", f"origin/{branch}", "-1", "--format=%H|%s"], cwd=folder_path)
            
            if latest_info:
                commit_hash, commit_msg = latest_info.split("|", 1)
                print(f"Cairo latest on {branch}: ({commit_hash[:8]}) \"{commit_msg}\"")
                
                if not auto_accept:
                    choice = input(f"Pull this commit for {folder}? [y/N]: ").lower()
                    if choice != 'y':
                        continue

                run_git_command(["checkout", commit_hash], cwd=folder_path)
                run_git_command(["add", folder], cwd=root_dir)
                print(f"✅ {folder} updated to {commit_hash[:8]} and staged.")
            else:
                print(f"❌ Could not retrieve latest commit info for {folder}.")
            continue

        # VERSIONED SEARCH LOGIC for other mods
        search_versions: List[str] = []
        for p in range(latest_patch, -1, -1):
            base = f"{major_minor}.{p}"
            search_versions.append(base)
            for rc in range(15, 0, -1):
                search_versions.append(f"{base}-rc.{rc}")
            for pre in range(25, 0, -1):
                search_versions.append(f"{base}-pre.{pre}")

        found: bool = False
        for version_num in search_versions:
            full_search_query: str = f"Updated to Version {version_num}"
            # Format: hash|subject
            commit_info: Optional[str] = run_git_command(
                ["log", "--all", f"--grep=^{full_search_query}$", "-n", "1", "--format=%H|%s"],
                cwd=folder_path
            )

            if commit_info:
                commit_hash, commit_msg = commit_info.split("|", 1)
                print(f"Found match: '{commit_msg}' ({commit_hash[:8]})")
                
                if not auto_accept:
                    choice = input(f"Apply this update to {folder}? [y/N]: ").lower()
                    if choice != 'y':
                        found = True
                        break
                
                run_git_command(["checkout", commit_hash], cwd=folder_path)
                run_git_command(["add", folder], cwd=root_dir)
                print(f"✅ {folder} updated and staged.")
                found = True
                break
        
        if not found:
            print(f"❌ No version found for {folder} matching '{major_minor}.x' logic.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Update submodules based on versions or latest branch.")
    parser.add_argument("version", help="Target version (e.g., 1.21.6)")
    parser.add_argument("-y", "--yes", action="store_true", help="Auto-accept all")
    
    args = parser.parse_args()

    submodules: List[str] = [
        "VintagestoryAPI", 
        "VSCreativeMod", 
        "VSEssentials", 
        "VSSurvivalMod", 
        "Cairo"
    ]

    update_submodules(input_string=args.version, submodule_dirs=submodules, auto_accept=args.yes)
    print("\nProcess complete.")