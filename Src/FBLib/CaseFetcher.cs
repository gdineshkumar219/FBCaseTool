// -------------------------------------------------------------------------------------
// CaseFetcher.cs
// Fetches a FogBugz case through the shared FogBugzClient, parses it, optionally downloads
// its attachments and writes case_details.md into a Case_<id> folder under a chosen root.
// Ported from the standalone FBFetch tool and adapted to reuse FBLib's client.
// -------------------------------------------------------------------------------------
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FBLib;

#region enum EAttachmentMode ----------------------------------------------------------------------
/// <summary>Whether a fetch downloads the case's attachments.</summary>
public enum EAttachmentMode {
   /// <summary>Fetch the case information without any attachments.</summary>
   None,
   /// <summary>Fetch all attachments associated with the case.</summary>
   All,
}
#endregion

#region class CaseFetchResult ---------------------------------------------------------------------
/// <summary>The outcome of a successful <see cref="CaseFetcher.Fetch"/>.</summary>
public sealed class CaseFetchResult {
   /// <summary>The resolved case number.</summary>
   public string CaseId { get; init; } = "";
   /// <summary>The case title.</summary>
   public string Title { get; init; } = "";
   /// <summary>The Case_&lt;id&gt; folder the case was written to.</summary>
   public string OutputFolder { get; init; } = "";
   /// <summary>The full path of the written case_details.md.</summary>
   public string MarkdownPath { get; init; } = "";
   /// <summary>How many attachments were saved.</summary>
   public int AttachmentsSaved { get; init; }
   /// <summary>How many attachments the case has.</summary>
   public int AttachmentsTotal { get; init; }
}
#endregion

#region class CaseFetcher -------------------------------------------------------------------------
/// <summary>Retrieves a FogBugz case and writes it as a self-contained markdown folder.</summary>
public sealed class CaseFetcher {
   #region Constructor ----------------------------------------------
   /// <summary>Creates a fetcher that talks to FogBugz through the given client.</summary>
   public CaseFetcher (FogBugzClient client) => mClient = client;
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Fetches <paramref name="caseId"/> into a Case_&lt;id&gt; folder under
   /// <paramref name="outputRoot"/>, honouring the attachment <paramref name="mode"/> and
   /// reporting progress lines through the optional <paramref name="progress"/> callback.
   /// Throws <see cref="FogBugzException"/> on a bad case, API/network error or write failure.</summary>
   public CaseFetchResult Fetch (string caseId, string outputRoot, EAttachmentMode mode, Action<string>? progress = null) {
      caseId = (caseId ?? "").Trim ();
      if (caseId.Length == 0) throw new FogBugzException ("Enter a case number.");
      if (string.IsNullOrWhiteSpace (outputRoot)) throw new FogBugzException ("Choose an output folder.");

      progress?.Invoke ($"Fetching case {caseId} from {mClient.BaseUrl} ...");
      var data = Parse (mClient.FetchCaseDetails (caseId), caseId);

      string caseDir = Path.Combine (outputRoot, $"Case_{data.CaseId}");
      try {
         Directory.CreateDirectory (caseDir);
      } catch (Exception ex) {
         throw new FogBugzException ($"Could not create output folder '{caseDir}': {ex.Message}");
      }

      int total = data.Attachments.Count, saved = 0;
      if (mode == EAttachmentMode.All && total > 0) saved = DownloadAttachments (data, caseDir, progress);

      string outFile = Path.Combine (caseDir, "case_details.md");
      try {
         File.WriteAllText (outFile, CaseMarkdown.Build (data), Encoding.UTF8);
      } catch (Exception ex) {
         throw new FogBugzException ($"Could not write '{outFile}': {ex.Message}");
      }
      progress?.Invoke ($"Written {outFile}");

      return new () {
         CaseId = data.CaseId, Title = data.Title, OutputFolder = caseDir,
         MarkdownPath = outFile, AttachmentsSaved = saved, AttachmentsTotal = total,
      };
   }
   #endregion

   #region Implementation -------------------------------------------
   // Parses the search response into a CaseDetails, extracting the reporter, structured
   // sections, the full conversation and the attachment list.
   CaseDetails Parse (XDocument doc, string caseId) {
      mNode = doc.Descendants ("case").FirstOrDefault ()
         ?? throw new FogBugzException ($"Case {caseId} was not found in FogBugz.");
      mEvents = mNode.Descendants ("event").ToList ();
      var openEv = mEvents.FirstOrDefault (e => (e.Element ("sVerb")?.Value ?? "").ToLower () is "opened" or "")
                ?? mEvents.FirstOrDefault ();

      string reporter = ResolveReporter (openEv);
      string bodyRaw = openEv?.Element ("s")?.Value.Trim () ?? "";
      var sec = CaseBodyParser.Parse (bodyRaw);
      bool unstruct = new[] { "description", "steps", "expected", "actual" }
                         .All (k => string.IsNullOrWhiteSpace (sec.GetValueOrDefault (k)));

      // Always capture the complete case conversation (every event) so nothing outside the
      // structured fields is missed.
      string conversation = BuildConversation (bodyRaw);

      var attachments = doc.Descendants ("attachment")
         .Select (a => (Name: a.Element ("sFileName")?.Value.Trim () ?? "", Url: a.Element ("sURL")?.Value.Trim () ?? ""))
         .Where (a => !string.IsNullOrEmpty (a.Name)).ToList ();

      return new CaseDetails {
         CaseId = Get ("ixBug", caseId), Title = Get ("sTitle"),
         Reporter = reporter, DateReported = CaseFields.FormatDate (Get ("dtOpened")),
         Priority = CaseFields.Priority (Get ("sPriority", "Normal")),
         Status = CaseFields.Status (Get ("sStatus", "Active")),
         CaseType = CaseFields.Category (Get ("sCategory", "Bug")),
         Assignee = Get ("sPersonAssignedTo", "Unassigned"),
         Project = Get ("sProject"), Area = Get ("sArea"),
         Milestone = Get ("sFixFor"), CategoryRaw = Get ("sCategory", "Bug"),
         Description = sec.GetValueOrDefault ("description", ""),
         Steps = sec.GetValueOrDefault ("steps", ""),
         Expected = sec.GetValueOrDefault ("expected", ""),
         Actual = sec.GetValueOrDefault ("actual", ""),
         Environ = sec.GetValueOrDefault ("environment", ""),
         Version = sec.GetValueOrDefault ("version", ""),
         Attachments = attachments,
         IsUnstructured = unstruct,
         FullConversation = conversation,
      };
   }

   // Downloads every attachment into the case folder, returning how many were saved. A failed
   // download is reported and skipped so one bad attachment does not abort the whole fetch.
   int DownloadAttachments (CaseDetails d, string dir, Action<string>? progress) {
      var used = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
      int ok = 0;
      progress?.Invoke ($"Downloading {d.Attachments.Count} attachment(s) ...");
      for (int i = 0; i < d.Attachments.Count; i++) {
         var (name, url) = d.Attachments[i];
         if (string.IsNullOrWhiteSpace (url)) continue;
         string safe = MakeUnique (Sanitize (name, i), used);
         try {
            var bytes = mClient.DownloadAttachment (url);
            if (bytes.Length == 0) { progress?.Invoke ($"'{name}': empty response, skipped"); continue; }
            File.WriteAllBytes (Path.Combine (dir, safe), bytes);
            d.Attachments[i] = (safe, url);
            ok++;
            progress?.Invoke ($"Saved {safe} ({bytes.Length:n0} bytes)");
         } catch (Exception ex) {
            progress?.Invoke ($"Failed '{name}': {ex.Message}");
         }
      }
      progress?.Invoke ($"Saved {ok}/{d.Attachments.Count} attachment(s)");
      return ok;
   }

   // Strips characters invalid in a file name and the ".unsafe" suffix FogBugz appends to risky
   // uploads; falls back to attachment_<n> when the name is empty.
   static string Sanitize (string name, int index) {
      name = (name ?? "").Trim ();
      while (name.EndsWith (".unsafe", StringComparison.OrdinalIgnoreCase))
         name = name[..^".unsafe".Length];
      foreach (char c in Path.GetInvalidFileNameChars ()) name = name.Replace (c, '_');
      return string.IsNullOrWhiteSpace (name) ? $"attachment_{index + 1}" : name;
   }

   // Disambiguates names that repeat within this fetch by suffixing _2, _3, ...
   static string MakeUnique (string name, HashSet<string> used) {
      string stem = Path.GetFileNameWithoutExtension (name), ext = Path.GetExtension (name), candidate = name;
      for (int n = 2; used.Contains (candidate); n++)
         candidate = $"{stem}_{n}{ext}";
      used.Add (candidate);
      return candidate;
   }

   // Reads an element's trimmed text with an optional default.
   string Get (string tag, string def = "") => mNode!.Element (tag)?.Value.Trim () ?? def;

   // Resolves the reporter from the opening event, falling back to its description text.
   static string ResolveReporter (XElement? ev) {
      if (ev == null) return "Unknown";
      string p = ev.Element ("sPerson")?.Value.Trim () ?? "";
      if (!string.IsNullOrEmpty (p)) return p;
      var m = Regex.Match (ev.Element ("evtDescription")?.Value ?? "", @"(?:opened|created)\s+by\s+(.+)", RegexOptions.IgnoreCase);
      return m.Success ? m.Groups[1].Value.Trim () : "Unknown";
   }

   // Builds the full conversation from every event, folding its change summary and note body
   // together and skipping only genuinely empty events.
   string BuildConversation (string fallback) {
      var parts = new List<string> ();
      foreach (var e in mEvents) {
         string body    = e.Element ("s")?.Value.Trim () ?? "";
         string changes = e.Element ("evtChanges")?.Value.Trim ()
                       ?? e.Element ("sChanges")?.Value.Trim () ?? "";
         string desc    = e.Element ("evtDescription")?.Value.Trim () ?? "";
         string person  = e.Element ("sPerson")?.Value.Trim () ?? "";
         string verb    = e.Element ("sVerb")?.Value.Trim () ?? "";
         string date    = CaseFields.FormatDate (e.Element ("dt")?.Value.Trim () ?? "");

         var lines = new List<string> ();
         if (changes.Length > 0) lines.Add (changes);
         if (body.Length > 0) lines.Add (body);
         string content = string.Join ("\n", lines).Trim ();

         if (content.Length == 0 && desc.Length == 0) continue;

         string who   = person.Length > 0 ? person : (desc.Length > 0 ? desc : "System");
         string label = verb.Length   > 0 ? verb   : (desc.Length > 0 ? desc : "event");
         string hdr   = $"### {who} ({label}) - {date}";
         parts.Add (content.Length > 0 ? $"{hdr}\n{content}" : $"{hdr}\n{desc}");
      }

      string result = string.Join ("\n\n---\n\n", parts).Trim ();
      return string.IsNullOrWhiteSpace (result) ? fallback : result;
   }
   #endregion

   #region Private data ---------------------------------------------
   readonly FogBugzClient mClient;
   XElement? mNode;
   List<XElement> mEvents = [];
   #endregion
}
#endregion
