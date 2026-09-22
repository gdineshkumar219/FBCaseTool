# FBCaseTool

FBCaseTool is a simple Windows app for working with a **FogBugz** case. A mode toggle at the
top of the window switches between two panes:

- **Update** — add a **comment** and change the case's **assignee** and **status**, then save the
  change straight back to FogBugz.
- **Fetch** — download a case (and optionally its attachments) to a local `Case_<id>` folder.

## How to use

1. Start FBCaseTool.
2. Type the FogBugz **case number** in the **Case** box and click **Load** (or press Enter).
   The case title and its current assignee and status are shown.
3. Type your **Comment** (optional).
4. Pick an **Assignee** from the dropdown (optional).
5. Pick a **Status** from the dropdown (optional).
6. Review your changes, then click **OK**. The dialog shows "Working…" while it saves.
7. The case is updated in FogBugz and the window closes.

To discard everything and leave the case unchanged, click **Cancel** or press **Esc**.

## Running the application

If FBCaseTool is installed in `C:\Metamation\FBCaseTool`, start it in either of these ways:

- Open the folder in File Explorer and double-click **FBCaseTool.exe**, or
- Run it from a command prompt:

  ```
  C:\Metamation\FBCaseTool\FBCaseTool.exe
  ```

To open the app with a case already loaded, add the case number:

```
C:\Metamation\FBCaseTool\FBCaseTool.exe 12345
```

To open in Fetch mode, add `--fetch`:

```
C:\Metamation\FBCaseTool\FBCaseTool.exe 12345 --fetch
```

## Common usage

- **Load a case** — type the case number and click **Load** (or press Enter).
- **Add a comment** — type your note in the **Comment** box.
- **Change the assignee** — choose a name from the **Assignee** list.
- **Change the status** — choose a value from the **Status** list.
- **Confirm the changes** — click **OK** to save them to FogBugz.
- **Cancel** — click **Cancel**, press **Esc**, or close the window; nothing is changed.

## Fetch a case

Switch the toggle at the top of the window to **Fetch** (or start the app with `--fetch`).
Enter the case number, choose an **Output folder** with **Browse**, pick whether to include
**Attachments** (**All** or **None**), and click **Fetch**. The case is written to
`<output folder>\Case_<ID>\case_details.md`, with any attachments saved alongside it. The
output folder you pick is remembered and offered again the next time Fetch mode is used.
Use **Open Folder** to reveal the result in File Explorer, or switch back to **Update** at
any time.

## Requirements

- Windows
- .NET 10 Desktop Runtime
- A FogBugz account and API token
- A `config.json` file next to `FBCaseTool.exe` containing your FogBugz address and API
  token. If it isn't set up yet, copy the included `config.example.json` to `config.json`
  and fill in your FogBugz URL and API token.
