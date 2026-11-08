Run `python Tests/IrregularSeedChecks/run.py` with Python 3 and the .NET 8 SDK on PATH.
The harness compiles the production Irregular rules, solver, analyzer, generator and model.
Unity logging and unrelated repository services are stubbed; this does not replace Unity play-mode testing.

Checks cover seed reproducibility (including integer edge cases), nine connected regions of nine cells,
layout diversity, valid solutions, uniqueness, logical analysis, seed-aware save validation, candidates
on resume, legacy fixed-layout saves, and isolation between puzzle instances.

Layout version 1 uses an explicit xorshift PRNG, bounded connected boundary swaps, and a bounded
solvability search. It retains a proven solution for inexpensive puzzle retries. Layout selection does
not depend on difficulty, clue removal attempts, or the global Unity random state. If all bounded
layout candidates fail, it uses a seeded transform of the known-solvable initial layout.

Save files contain PuzzleSeed and IrregularLayoutVersion, not a region map. Version 0 restores the
previous fixed layout. Do not change version 1's algorithm or its solver traversal without retaining
that implementation for regeneration of existing seeds. Future version changes must also account
for seed-only DB history/replays; the save-file version alone does not version DB history.
