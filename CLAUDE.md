@AGENTS.md

# Claude Code specifics

Everything in `AGENTS.md` applies.

- **Language**: answer the owner in Brazilian Portuguese; write code, commits and docs in English.
- **Shells**: Windows. The Bash tool is Git Bash (use it for `sed`/`grep` edits). Never pipe source files through
  PowerShell `Get-Content | Set-Content` (UTF-8 damage).
- Never push, tag or publish a release without the owner's word.
