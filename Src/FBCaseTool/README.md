# FBCaseTool — Update / Fetch FogBugz Case dialog

A small **standalone** WPF app for updating a FogBugz case. It provides a clean **Update Case**
window for reviewing and changing a case's **comment**, **assignee** and **status** before
explicitly confirming the update. It talks to FogBugz directly via the shared **FBLib** library.
It can also **fetch** a case — saving its details as markdown and downloading its attachments —
into an output folder you choose.

This is a trimmed-down sibling of **FBCaseUpdater**: same Update / Fetch behaviour, no Release
Note mode (no Word / `Flux.Main.docx` dependency).

```
┌─────────────────────────────────────┐
│        Update FogBugz Case          │
│ Case 12345: <title>                 │
│ Comment:                            │
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
│ Assignee: [ Current Assignee    ▼ ] │
│ Status:   [ Current Status      ▼ ] │
│                  [Cancel] [  OK  ]  │
└─────────────────────────────────────┘
```

## How it fits together

FBCaseTool is a **standalone** WPF application. It talks to FogBugz **directly** through
the shared **FBLib** library:

```
FBCaseTool (WPF)
 ├── Presentation      App.xaml, MainWindow.xaml (Update / Fetch toggle),
 │                    UpdateCaseView.xaml, FetchCaseView.xaml
 ├── ViewModel         MainViewModel, UpdateCaseViewModel, FetchCaseVM,
 │                    ObservableObject, RelayCommand
 └── FogBugz service   FBLib.FogBugzClient (config, QueryCase, UpdateCase),
                       FBLib.CaseFetcher  (fetch case + attachments -> markdown)
```

The main window shows an **Update / Fetch** toggle at the top and swaps its content
between the two panes in place — no separate window is opened.

- On open it loads the case (current status/assignee + the status and people lists)
  via `FogBugzClient.QueryCase`.
- **OK** validates the input and calls `FogBugzClient.UpdateCase`. On success the window
  closes; on failure the error is shown and the window stays open to retry.
- **Cancel** or closing the window makes **no change** to the case.

## Fetch a case

Switch the toggle at the top of the window to **Fetch** (or start the app with `--fetch`).
Enter the case number, choose an **Output folder** with **Browse**, pick whether to include
**Attachments** (**All** or **None**), and click **Fetch**. The case is written to
`<output folder>\Case_<ID>\case_details.md`, with any attachments saved alongside it. The
output folder you pick is remembered and offered again the next time Fetch mode is used.
Use **Open Folder** to reveal the result in File Explorer, or switch back to **Update** at
any time.

## Arguments

| Argument | Description |
|---|---|
| `<CASE_ID>` | The FogBugz case to load initially (optional — can be typed in the dialog) |
| `--config <path>` | Path to the `config.json` to use (FogBugz URL + token) |
| `--comment-draft <text>` | Prefill the comment box |
| `--fetch` | Start in Fetch mode instead of Update mode |

If `--config` is not given, FBCaseTool reads `config.json` from its own folder (copy
`config.example.json` and fill in the token, or set the `FB_API_TOKEN` environment
variable).

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Case updated |
| `1` | Error (e.g. bad arguments) |
| `2` | Cancelled — no change |

## Build

```powershell
dotnet build Src\FBCaseTool.slnx -c Debug
```

## Requirements

- .NET 10 SDK, Windows (WPF)
- References the shared **FBLib** library for FogBugz access; otherwise only the .NET
  base class library.
