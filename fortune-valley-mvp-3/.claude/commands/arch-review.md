Run a systematic architecture review of all `.cs` files changed since the last commit.

Step 1: Run `git diff HEAD --name-only` and `git diff HEAD` to get the list and full diff of changed files. Filter to `.cs` files only.

Step 2: For each changed `.cs` file, evaluate it against every rule in the Architecture Principles Table in CLAUDE.md Section 4.1. For each rule, perform the exact detectable check described in the "Detectable Violation" column. Do not apply judgment -- only check for the exact strings, counts, or patterns specified.

Step 3: For each violation found, look up its Classification in the Architecture Principles Table (column 4). If the Classification is BLOCKING, add it to the BLOCKING list. If ADVISORY, add it to the ADVISORY list. Do not reclassify.

Step 4: Output in this exact format:

## BLOCKING Violations
[file:line] Rule name -- exact violation found
(or "None" if clean)

## ADVISORY Violations
[file:line] Rule name -- exact violation found
(or "None" if clean)

## Pre-write Declaration Required
List any new files that were written without a pre-write FILE: declaration block.

## Post-write Checklist Required
List any new files that were written without a post-write checklist output.

Do not suggest fixes. Only report violations. The developer decides how to resolve them.
