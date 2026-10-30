# Changelog

## 0.1.0 — 2026-10-30
- Window ▸ Mirven panel: connected Mirven Studio project, requests, tasks, task details.
- Selecting a task lists the files Mirven changed (and the planned files); clicking one pings it in the Project window.
- Compile errors are collected from `CompilationPipeline` and sent to Mirven (`POST /api/m1/studyo/<pid>/derleme`).
- Read-only: the package never writes to the Unity project.
