// -------------------------------------------------------------------------------------
// FogBugzClient.cs
// Talks to the FogBugz XML API: loads configuration, queries a case's current fields and
// the available statuses/people, and applies updates. Shared by FBUpdate and FBCaseUpdater.
// -------------------------------------------------------------------------------------
using System.Net;
using System.Text.Json;
using System.Xml.Linq;

namespace FBLib;

#region class FogBugzClient -----------------------------------------------------------------------
/// <summary>Talks to the FogBugz XML API: loads config, queries a case and applies updates.</summary>
public sealed class FogBugzClient {
   #region Constructor ----------------------------------------------
   FogBugzClient (string baseUrl, string token, HttpClientHandler handler) =>
      (mBaseUrl, mToken, mHandler) = (baseUrl, token, handler);
   #endregion

   #region Properties -----------------------------------------------
   /// <summary>The FogBugz instance base URL (without a trailing slash).</summary>
   public string BaseUrl => mBaseUrl;
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Creates a client from a config.json (the app's base directory by default),
   /// honouring the FB_API_TOKEN and FB_PROXY_PASSWORD environment overrides.</summary>
   public static FogBugzClient Create (string? configPath = null) {
      string configFile = configPath ?? Path.Join (AppContext.BaseDirectory, "config.json");
      if (!File.Exists (configFile)) throw new FogBugzException ($"Config not found: {configFile}");
      var cfg = JsonDocument.Parse (File.ReadAllText (configFile)).RootElement;
      var fb = cfg.GetProperty ("fogbugz");
      string baseUrl = fb.GetProperty ("url").GetString ()!.TrimEnd ('/');
      string token = Environment.GetEnvironmentVariable ("FB_API_TOKEN")
                  ?? (fb.TryGetProperty ("api_token", out var t) ? t.GetString ()! : "");
      if (string.IsNullOrWhiteSpace (baseUrl) || string.IsNullOrWhiteSpace (token) || token == "YOUR_API_TOKEN_HERE")
         throw new FogBugzException ($"FogBugz URL or API token missing. Set FB_API_TOKEN or edit {configFile}.");
      return new (baseUrl, token, BuildHandler (cfg));
   }

   /// <summary>Loads a case's current fields plus the statuses and people needed to edit it.</summary>
   public CaseInfo QueryCase (string caseId) {
      var caseNode = SearchCase (caseId) ?? throw new FogBugzException ($"Case {caseId} was not found in FogBugz.");
      string category = caseNode.Element ("ixCategory")?.Value ?? "";
      var statuses = ListStatuses (category);
      if (statuses.Count == 0) throw new FogBugzException ("Could not retrieve any statuses from FogBugz.");
      return new () {
         CaseId = caseId,
         Title = caseNode.Element ("sTitle")?.Value ?? "",
         CurrentStatus = caseNode.Element ("sStatus")?.Value ?? "",
         CurrentAssignee = caseNode.Element ("sPersonAssignedTo")?.Value ?? "",
         Category = category,
         Statuses = statuses,
         People = ListPeople (),
      };
   }

   /// <summary>Applies the given changes to a case and returns the resolved case number;
   /// throws <see cref="FogBugzException"/> on an API or network error.</summary>
   public string UpdateCase (string caseId, string cmd, string? comment, string? assignee, string? priority, string? status, string? title) {
      var form = new List<KeyValuePair<string, string>> { new ("cmd", cmd), new ("token", mToken), new ("ixBug", caseId) };
      if (comment  != null) form.Add (new ("sEvent", comment));
      if (assignee != null) form.Add (new ("sPersonAssignedTo", assignee));
      if (priority != null) form.Add (new ("ixPriority", priority));
      if (status   != null) form.Add (new ("sStatus", status));
      if (title    != null) form.Add (new ("sTitle", title));
      var doc = Post (form);
      Throw (doc);
      return doc.Descendants ("case").FirstOrDefault ()?.Attribute ("ixBug")?.Value ?? caseId;
   }

   /// <summary>Fetches a case's full detail (all columns plus its event history), used by
   /// <see cref="CaseFetcher"/> to export the case; throws <see cref="FogBugzException"/> on error.</summary>
   internal XDocument FetchCaseDetails (string caseId) {
      const string cols = "ixBug,sTitle,sStatus,sPersonAssignedTo,sPriority,sProject,sArea,sFixFor,sCategory,dtOpened,events";
      var doc = Post ([new ("cmd", "search"), new ("token", mToken), new ("q", caseId), new ("cols", cols)]);
      Throw (doc);
      return doc;
   }

   /// <summary>Downloads an attachment from the sURL returned by the API and returns its bytes.
   /// The URL arrives HTML-encoded and without the token, so it is decoded and the token appended.</summary>
   internal byte[] DownloadAttachment (string url) {
      url = WebUtility.HtmlDecode (url);
      string full = url.StartsWith ("http", StringComparison.OrdinalIgnoreCase) ? url : $"{mBaseUrl}/{url.TrimStart ('/')}";
      if (!full.Contains ("token=", StringComparison.OrdinalIgnoreCase))
         full += (full.Contains ('?') ? "&" : "?") + "token=" + WebUtility.UrlEncode (mToken);
      using var http = new HttpClient (mHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds (AttachmentTimeoutSeconds) };
      http.DefaultRequestHeaders.TryAddWithoutValidation ("User-Agent", "FBCaseUpdater/1.0");
      http.DefaultRequestHeaders.TryAddWithoutValidation ("Accept", "*/*");
      try {
         var resp = http.GetAsync (full).GetAwaiter ().GetResult ();
         var bytes = resp.Content.ReadAsByteArrayAsync ().GetAwaiter ().GetResult ();
         if (!resp.IsSuccessStatusCode) throw new FogBugzException ($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
         return bytes;
      } catch (FogBugzException) { throw; } catch (Exception ex) { throw new FogBugzException ($"Network error: {ex.Message}"); }
   }
   #endregion

   #region Implementation -------------------------------------------
   // Builds the HttpClientHandler, honouring an optional proxy from the config.
   static HttpClientHandler BuildHandler (JsonElement cfg) {
      var handler = new HttpClientHandler { UseDefaultCredentials = true, PreAuthenticate = true };
      if (cfg.TryGetProperty ("proxy", out var px)) {
         string pu = px.TryGetProperty ("url", out var v1) ? v1.GetString ()! : "";
         string un = px.TryGetProperty ("username", out var v2) ? v2.GetString ()! : "";
         string pw = Environment.GetEnvironmentVariable ("FB_PROXY_PASSWORD")
                  ?? (px.TryGetProperty ("password", out var v3) ? v3.GetString ()! : "");
         if (!string.IsNullOrWhiteSpace (pu)) {
            handler.Proxy = new WebProxy (pu) {
               Credentials = !string.IsNullOrWhiteSpace (un) && !string.IsNullOrWhiteSpace (pw)
                  ? new NetworkCredential (un, pw) : CredentialCache.DefaultCredentials
            };
            handler.UseProxy = true;
         }
      }
      return handler;
   }

   // Looks up the case by id, returning its <case> node or null when it does not exist.
   XElement? SearchCase (string caseId) {
      var doc = Post ([new ("cmd", "search"), new ("token", mToken), new ("q", caseId), new ("cols", "ixCategory,sStatus,sTitle,sPersonAssignedTo")]);
      Throw (doc);
      return doc.Descendants ("case").FirstOrDefault ();
   }

   // Retrieves the available statuses for a category (never hardcoded).
   List<string> ListStatuses (string category) {
      var form = new List<KeyValuePair<string, string>> { new ("cmd", "listStatuses"), new ("token", mToken) };
      if (!string.IsNullOrWhiteSpace (category)) form.Add (new ("ixCategory", category));
      var doc = Post (form);
      Throw (doc);
      return [.. doc.Descendants ("status")
                .Select (s => s.Element ("sStatus")?.Value?.Trim () ?? "")
                .Where (s => s.Length > 0)
                .Distinct ()];
   }

   // Retrieves the list of active people used to populate the assignee list.
   List<Person> ListPeople () {
      var doc = Post ([new ("cmd", "listPeople"), new ("token", mToken)]);
      Throw (doc);
      return [.. doc.Descendants ("person")
                .Select (p => new Person {
                   Name = p.Element ("sFullName")?.Value?.Trim () ?? "",
                   Email = p.Element ("sEmail")?.Value?.Trim () ?? "",
                })
                .Where (p => p.Name.Length > 0)];
   }

   // Posts a form to the FogBugz API and returns the parsed XML response.
   XDocument Post (IEnumerable<KeyValuePair<string, string>> form) {
      string url = $"{mBaseUrl}/api.asp";
      using var http = new HttpClient (mHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds (RequestTimeoutSeconds) };
      try {
         var resp = http.PostAsync (url, new FormUrlEncodedContent (form)).GetAwaiter ().GetResult ();
         string body = resp.Content.ReadAsStringAsync ().GetAwaiter ().GetResult ();
         return XDocument.Parse (body);
      } catch (Exception ex) { throw new FogBugzException ($"Network error: {ex.Message}"); }
   }

   // Throws a FogBugzException when the response carries an <error> element.
   static void Throw (XDocument doc) {
      var err = doc.Descendants ("error").FirstOrDefault ();
      if (err != null) throw new FogBugzException (err.Value.Trim ());
   }
   #endregion

   #region Private data ---------------------------------------------
   const int RequestTimeoutSeconds = 30;
   const int AttachmentTimeoutSeconds = 120;
   readonly string mBaseUrl, mToken;
   readonly HttpClientHandler mHandler;
   #endregion
}
#endregion
