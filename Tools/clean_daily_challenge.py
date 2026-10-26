#!/usr/bin/env python3
"""Delete the newest Daily Challenge test row and rebuild its aggregate stats.

By default, the database is read from the Windows Unity Editor path:
  %USERPROFILE%\\AppData\\LocalLow\\plackkanickal\\Sudoku\\game_history.db

Pass --db when testing a standalone build or another Unity data directory.
The game must be closed before running this script.
"""

from __future__ import annotations

import argparse
import os
import shutil
import sqlite3
import sys
from datetime import datetime
from pathlib import Path


STATS_FIELDS = {
    0: "SimpleBestTimeGameId",
    1: "BeginnerBestTimeGameId",
    2: "EasyBestTimeGameId",
    3: "NoviceBestTimeGameId",
    4: "ModerateBestTimeGameId",
    5: "AdvancedBestTimeGameId",
    6: "HardBestTimeGameId",
    7: "ExpertBestTimeGameId",
    8: "HardestBestTimeGameId",
}


def default_database_path() -> Path:
    user_profile = os.environ.get("USERPROFILE")
    if not user_profile:
        raise RuntimeError("Set --db to the path of game_history.db.")
    return (
        Path(user_profile)
        / "AppData"
        / "LocalLow"
        / "plackkanickal"
        / "Sudoku"
        / "game_history.db"
    )


def table_columns(connection: sqlite3.Connection, table: str) -> set[str]:
    return {row[1] for row in connection.execute(f'PRAGMA table_info("{table}")')}


def require_schema(connection: sqlite3.Connection) -> None:
    required = {
        "DailyGame_challenge": {"Id", "CurrentDate", "Difficulty", "ElapsedSeconds", "IsWon", "CompletedAt"},
        "DailyGameChallengeStats": {
            "Id", "FirstStartedAt", "CurrentStreakStartedAt", "CurrentStreak",
            "LastPlayedAt", "LastLostAt", "GamesPlayed", "GamesWon",
            "BestTimeSeconds", "BestTimeGameId", *STATS_FIELDS.values(),
        },
    }
    for table, expected in required.items():
        try:
            actual = table_columns(connection, table)
        except sqlite3.Error as exc:
            raise RuntimeError(f"Could not inspect table {table}: {exc}") from exc
        missing = expected - actual
        if missing:
            raise RuntimeError(
                f"Table {table} is missing columns: {', '.join(sorted(missing))}. "
                "No changes were made."
            )


def rebuild_stats(connection: sqlite3.Connection) -> None:
    stats = connection.execute(
        "SELECT FirstStartedAt FROM DailyGameChallengeStats WHERE Id = 1"
    ).fetchone()
    if stats is None:
        connection.execute(
            "INSERT INTO DailyGameChallengeStats (Id) VALUES (1)"
        )
        first_started_at = None
    else:
        first_started_at = stats[0]

    all_rows = connection.execute(
        "SELECT Id FROM DailyGame_challenge ORDER BY Id"
    ).fetchall()
    if not all_rows:
        first_started_at = None

    completed = connection.execute(
        "SELECT Id, Difficulty, ElapsedSeconds, IsWon, CompletedAt "
        "FROM DailyGame_challenge "
        "WHERE CompletedAt IS NOT NULL AND TRIM(CAST(CompletedAt AS TEXT)) <> '' "
        "ORDER BY Id"
    ).fetchall()

    games_played = len(completed)
    games_won = sum(bool(row[3]) for row in completed)
    current_streak = 0
    streak_started_at = None
    last_played_at = completed[-1][4] if completed else None
    last_lost_at = None
    for _game_id, _difficulty, _seconds, is_won, completed_at in completed:
        if is_won:
            if current_streak == 0:
                streak_started_at = completed_at
            current_streak += 1
        else:
            current_streak = 0
            streak_started_at = None
            last_lost_at = str(completed_at)[:10]

    # If the completed history ends in a loss, the active streak has no start.
    if current_streak == 0:
        streak_started_at = None

    wins = [row for row in completed if bool(row[3])]
    best = min(wins, key=lambda row: (float(row[2]), int(row[0]))) if wins else None
    per_difficulty: dict[str, int | None] = {field: None for field in STATS_FIELDS.values()}
    for difficulty, field in STATS_FIELDS.items():
        candidates = [row for row in wins if int(row[1]) == difficulty]
        if candidates:
            per_difficulty[field] = int(
                min(candidates, key=lambda row: (float(row[2]), int(row[0])))[0]
            )

    values = {
        "FirstStartedAt": first_started_at,
        "CurrentStreakStartedAt": streak_started_at,
        "CurrentStreak": current_streak,
        "LastPlayedAt": last_played_at,
        "LastLostAt": last_lost_at,
        "GamesPlayed": games_played,
        "GamesWon": games_won,
        "BestTimeSeconds": float(best[2]) if best else None,
        "BestTimeGameId": int(best[0]) if best else None,
        **per_difficulty,
    }
    assignments = ", ".join(f'"{column}" = ?' for column in values)
    connection.execute(
        f"UPDATE DailyGameChallengeStats SET {assignments} WHERE Id = 1",
        tuple(values.values()),
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Remove the newest Daily Challenge row and recalculate its stats."
    )
    parser.add_argument(
        "--db",
        type=Path,
        help="game_history.db path (defaults to the Unity Editor data path on Windows)",
    )
    parser.add_argument(
        "--yes",
        action="store_true",
        help="skip the confirmation prompt",
    )
    args = parser.parse_args()

    try:
        database = args.db or default_database_path()
        if not database.is_file():
            raise RuntimeError(f"Database file not found: {database}")

        # Use SQLite's backup API so the backup includes any committed WAL data.
        connection = sqlite3.connect(database, timeout=5)
        connection.row_factory = sqlite3.Row
        require_schema(connection)
        latest = connection.execute(
            "SELECT Id, CurrentDate, Difficulty, CompletedAt "
            "FROM DailyGame_challenge ORDER BY Id DESC LIMIT 1"
        ).fetchone()
        if latest is None:
            print("DailyGame_challenge is already empty; nothing to clean.")
            return 0

        print(f"Database: {database}")
        print(
            "Latest row to delete: "
            f"Id={latest['Id']}, CurrentDate={latest['CurrentDate']}, "
            f"Difficulty={latest['Difficulty']}, CompletedAt={latest['CompletedAt']}"
        )
        if not args.yes:
            answer = input("Delete this row and rebuild DailyGameChallengeStats? [y/N] ")
            if answer.strip().lower() not in {"y", "yes"}:
                print("Cancelled; no changes were made.")
                return 0

        backup = database.with_name(
            f"{database.stem}.before_daily_challenge_cleanup_"
            f"{datetime.now().strftime('%Y%m%d_%H%M%S')}{database.suffix}"
        )
        with sqlite3.connect(backup) as backup_connection:
            connection.backup(backup_connection)

        try:
            connection.execute("BEGIN IMMEDIATE")
            connection.execute(
                "DELETE FROM DailyGame_challenge WHERE Id = ?", (latest["Id"],)
            )
            rebuild_stats(connection)
            connection.commit()
        except Exception:
            connection.rollback()
            backup.unlink(missing_ok=True)
            raise

        print(f"Deleted DailyGame_challenge row Id={latest['Id']}.")
        print("DailyGameChallengeStats now reflects the remaining completed rows.")
        print(f"Backup: {backup}")
        return 0
    except (OSError, sqlite3.Error, RuntimeError) as exc:
        print(f"Cleanup failed: {exc}", file=sys.stderr)
        return 1
    finally:
        if "connection" in locals():
            connection.close()


if __name__ == "__main__":
    raise SystemExit(main())
