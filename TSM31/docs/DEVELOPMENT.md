# Development workflow

This repo uses .NET Hot Reload while the app is running. Please follow these guidelines so we don’t interrupt the developer’s session:

- Do not run manual builds after editing code when Hot Reload is active.
- Assume the app is running under dotnet watch (Hot Reload) and will pick up supported changes automatically.
- If a change requires a restart (e.g., editing project files, resource dictionaries in ways Hot Reload can’t handle, major XAML tree changes), coordinate and ask before restarting.
- Prefer small, incremental edits to XAML and code-behind; verify in the running app.
- If you need to run any commands, share them as optional instructions instead of executing them.

What not to do
- Do not run: dotnet build, dotnet run, or kill the watch process.
- Do not change SDK versions or global.json without confirming first.

What you can do
- Edit files and save; Hot Reload will apply changes.
- Add new files (classes, view models, XAML) — Hot Reload usually supports this, but a restart may be needed for some additions.
- Log concise status updates about what changed and how to verify in the running app.

Notes
- If Hot Reload stops applying changes, report it and provide a short fallback plan (e.g., safe restart steps) but do not execute it yourself unless asked.
- Keep telemetry logging lightweight; it helps verify key handling and tab switching without excessive noise.