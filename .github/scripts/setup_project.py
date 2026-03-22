#!/usr/bin/env python3
"""
Setup GitHub project structure for Vox repository.

Creates milestones, assigns issues to milestones, sets up sub-issue
parent-child relationships, and updates issue bodies with dependency
information.

Usage:
  GH_TOKEN=<token> GITHUB_REPOSITORY=Achran/vox python3 setup_project.py
"""

import json
import os
import sys
import time
import urllib.error
import urllib.request

REPO = os.environ.get("GITHUB_REPOSITORY", "Achran/vox")
TOKEN = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")

if not TOKEN:
    print("ERROR: No GitHub token found (set GH_TOKEN or GITHUB_TOKEN)")
    sys.exit(1)

HEADERS = {
    "Authorization": f"Bearer {TOKEN}",
    "Accept": "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    "Content-Type": "application/json",
}


def api(method: str, path: str, data: dict | None = None) -> dict | list | None:
    url = f"https://api.github.com{path}"
    body = json.dumps(data).encode() if data is not None else None
    req = urllib.request.Request(url, data=body, headers=HEADERS, method=method)
    try:
        with urllib.request.urlopen(req) as response:
            return json.loads(response.read())
    except urllib.error.HTTPError as e:
        error_body = e.read().decode()
        print(f"  ⚠  HTTP {e.code} for {method} {path}: {error_body[:500]}")
        return None


# Seconds to wait between API calls to avoid GitHub secondary rate limiting.
RATE_LIMIT_DELAY_SECONDS = 0.3


def rate_limit_delay() -> None:
    """Pause briefly between API calls to avoid secondary rate limiting."""
    time.sleep(RATE_LIMIT_DELAY_SECONDS)


# ---------------------------------------------------------------------------
# Milestones
# ---------------------------------------------------------------------------

MILESTONES_DEF = [
    {
        "title": "Milestone 1 – Auth & Uživatelé",
        "description": "Registrace, přihlášení, JWT, OAuth poskytovatelé, propojení účtů a auth UI.",
    },
    {
        "title": "Milestone 2 – Servery a kanály",
        "description": "Doménové entity, CRUD API, UI pro servery, kanály a členství.",
    },
    {
        "title": "Milestone 3 – Textový chat",
        "description": "Real-time textový chat přes SignalR, perzistence zpráv a chat UI.",
    },
    {
        "title": "Milestone 4 – Hlasová komunikace",
        "description": "WebRTC signaling, LiveKit media server, voice UI a testy.",
    },
    {
        "title": "Milestone 5 – Online presence",
        "description": "SignalR presence tracking, heartbeat, UI online uživatelů a testy.",
    },
]

# milestone title → GitHub milestone number
MILESTONE_MAP: dict[str, int] = {}


def setup_milestones() -> None:
    print("\n── Milestones ──────────────────────────────────────────────────────")

    # Fetch existing milestones to avoid duplicates
    existing: list = api("GET", f"/repos/{REPO}/milestones?state=all&per_page=100") or []
    existing_by_title = {m["title"]: m["number"] for m in existing}

    for ms in MILESTONES_DEF:
        title = ms["title"]
        if title in existing_by_title:
            MILESTONE_MAP[title] = existing_by_title[title]
            print(f"  ✓ Already exists #{existing_by_title[title]}: {title}")
            continue

        result = api("POST", f"/repos/{REPO}/milestones", ms)
        rate_limit_delay()
        if result and "number" in result:
            MILESTONE_MAP[title] = result["number"]
            print(f"  ✓ Created #{result['number']}: {title}")
        else:
            print(f"  ✗ Failed to create: {title}")


# ---------------------------------------------------------------------------
# Issue → Milestone assignment
# ---------------------------------------------------------------------------

MILESTONE_ISSUES: dict[str, list[int]] = {
    "Milestone 1 – Auth & Uživatelé":     [3, 8, 9, 10, 11, 12],
    "Milestone 2 – Servery a kanály":     [6, 13, 14, 15, 16],
    "Milestone 3 – Textový chat":         [4, 17, 18, 19],
    "Milestone 4 – Hlasová komunikace":   [5, 20, 21, 22, 23],
    "Milestone 5 – Online presence":      [7, 24, 25, 26],
}


def assign_milestones() -> None:
    print("\n── Assigning issues to milestones ──────────────────────────────────")
    for ms_title, issues in MILESTONE_ISSUES.items():
        ms_num = MILESTONE_MAP.get(ms_title)
        if not ms_num:
            print(f"  ⚠  Milestone '{ms_title}' not found, skipping")
            continue
        for issue_num in issues:
            result = api(
                "PATCH",
                f"/repos/{REPO}/issues/{issue_num}",
                {"milestone": ms_num},
            )
            rate_limit_delay()
            if result:
                print(f"  ✓ Issue #{issue_num} → {ms_title}")
            else:
                print(f"  ✗ Failed: Issue #{issue_num} → {ms_title}")


# ---------------------------------------------------------------------------
# Sub-issues  (parent-child relationships)
# ---------------------------------------------------------------------------

# parent issue number → list of child issue numbers
SUB_ISSUES: dict[int, list[int]] = {
    3: [8, 9, 10, 11, 12],
    4: [17, 18, 19],
    5: [20, 21, 22, 23],
    6: [13, 14, 15, 16],
    7: [24, 25, 26],
}

# issue number → internal GitHub issue id (populated by _fetch_issue_ids)
ISSUE_IDS: dict[int, int] = {}


def _fetch_issue_ids(issue_numbers: list[int]) -> None:
    """Pre-fetch the internal GitHub issue IDs needed for the sub-issues API."""
    print("\n── Fetching issue IDs ──────────────────────────────────────────────")
    for num in issue_numbers:
        issue = api("GET", f"/repos/{REPO}/issues/{num}")
        rate_limit_delay()
        if issue and "id" in issue:
            ISSUE_IDS[num] = issue["id"]
            print(f"  ✓ Issue #{num} → internal id {issue['id']}")
        else:
            print(f"  ✗ Could not fetch id for issue #{num}")


def setup_sub_issues() -> None:
    # Collect all unique issue numbers involved in sub-issue relationships
    all_child_numbers = [child for children in SUB_ISSUES.values() for child in children]
    _fetch_issue_ids(all_child_numbers)

    print("\n── Sub-issues (parent → child) ─────────────────────────────────────")
    for parent_num, children in SUB_ISSUES.items():
        for child_num in children:
            child_id = ISSUE_IDS.get(child_num)
            if child_id is None:
                print(f"  ✗ No id for issue #{child_num}, skipping")
                continue
            result = api(
                "POST",
                f"/repos/{REPO}/issues/{parent_num}/sub_issues",
                {"sub_issue_id": child_id},
            )
            rate_limit_delay()
            if result:
                print(f"  ✓ #{child_num} (id={child_id}) is sub-issue of #{parent_num}")
            else:
                print(f"  ✗ Failed: #{child_num} sub-issue of #{parent_num}")


# ---------------------------------------------------------------------------
# Blocking dependencies  (update issue bodies with "Depends on" section)
# ---------------------------------------------------------------------------

# issue number → list of issue numbers it depends on
DEPENDS_ON: dict[int, list[int]] = {
    # Auth sub-tasks
    9:  [8],
    10: [9],
    11: [8, 9, 10],
    12: [8, 9, 10, 11],
    # Servers & Channels sub-tasks
    13: [8],
    14: [13],
    15: [14],
    16: [13, 14, 15],
    # Text chat sub-tasks (need auth + channels to exist)
    17: [14],
    18: [17],
    19: [17, 18],
    # Voice sub-tasks
    20: [14],
    21: [20],
    22: [21],
    23: [20, 21, 22],
    # Presence sub-tasks
    24: [14],
    25: [24],
    26: [24, 25],
    # High-level features
    4: [3, 6],
    5: [3, 6],
    6: [3],
    7: [3, 6],
}

DEPENDENCY_SECTION_MARKER = "<!-- vox-dependencies -->"


def _dependency_section(deps: list[int]) -> str:
    refs = ", ".join(f"#{d}" for d in deps)
    lines = [
        "",
        DEPENDENCY_SECTION_MARKER,
        "## Závislosti",
        f"Blocked by: {refs}",
    ]
    return "\n".join(lines)


def setup_dependencies() -> None:
    print("\n── Blocking dependencies (issue body updates) ──────────────────────")
    for issue_num, deps in DEPENDS_ON.items():
        # Fetch current body
        issue = api("GET", f"/repos/{REPO}/issues/{issue_num}")
        rate_limit_delay()
        if not issue:
            print(f"  ✗ Could not fetch issue #{issue_num}")
            continue

        body: str = issue.get("body") or ""

        # Remove any previous dependency section before re-writing
        if DEPENDENCY_SECTION_MARKER in body:
            body = body[: body.index(DEPENDENCY_SECTION_MARKER)].rstrip()

        new_body = body + _dependency_section(deps)

        result = api(
            "PATCH",
            f"/repos/{REPO}/issues/{issue_num}",
            {"body": new_body},
        )
        rate_limit_delay()
        if result:
            refs = ", ".join(f"#{d}" for d in deps)
            print(f"  ✓ Issue #{issue_num} blocked by {refs}")
        else:
            print(f"  ✗ Failed to update issue #{issue_num}")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    print(f"Setting up project structure for {REPO}")
    setup_milestones()
    assign_milestones()
    setup_sub_issues()
    setup_dependencies()
    print("\n✅ Done!")


if __name__ == "__main__":
    main()

