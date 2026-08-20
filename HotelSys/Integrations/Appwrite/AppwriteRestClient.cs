#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// عميل Appwrite خادمي صغير يعتمد REST الرسمي، ويغلف صيغة JSON queries
/// حتى لا تتكرر أخطاء pagination بين خدمات الكيانات.
/// </summary>
public sealed class AppwriteRestClient
{
    private readonly HttpClient _httpClient;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteRestClient> _logger;

    public AppwriteRestClient(
        HttpClient httpClient,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteRestClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppwriteDocumentPage> ListDocumentsAsync(
        string collectionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return new AppwriteDocumentPage(0, Array.Empty<AppwriteDocument>());
        }

        var pageSize = Math.Clamp(_options.PageSize, 1, 100);
        var all = new List<AppwriteDocument>();
        var offset = 0;
        var total = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var queries = new[]
            {
                JsonQuery("limit", pageSize),
                JsonQuery("offset", offset)
            };
            using var request = CreateRequest(HttpMethod.Get, DocumentsUrl(collectionId, queries));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Appwrite list {collectionId} failed ({(int)response.StatusCode}): {TrimError(body)}");
            }

            var page = JsonSerializer.Deserialize<AppwriteDocumentPage>(body, JsonOptions) ?? new AppwriteDocumentPage();
            total = page.Total;
            all.AddRange(page.Documents);

            if (page.Documents.Count == 0 || all.Count >= total)
            {
                break;
            }

            if (page.Documents.Count < pageSize)
            {
                throw new InvalidOperationException($"Appwrite collection {collectionId} reported {total} documents but pagination stopped at {all.Count}.");
            }

            offset += page.Documents.Count;
            if (offset > Math.Max(total, pageSize) + pageSize)
            {
                throw new InvalidOperationException($"Appwrite pagination guard stopped collection {collectionId} at offset {offset}.");
            }
        }

        if (all.Count != total)
        {
            throw new InvalidOperationException($"Appwrite collection {collectionId} reported {total} documents but {all.Count} were collected.");
        }

        _logger.LogDebug("Collected {Count} Appwrite documents from {CollectionId}.", all.Count, collectionId);
        return new AppwriteDocumentPage(total, all);
    }

    public async Task UpsertDocumentAsync(
        string collectionId,
        string documentId,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
    {
        var updateBody = JsonSerializer.Serialize(new { data }, JsonOptions);
        using var updateRequest = CreateRequest(HttpMethod.Put, DocumentUrl(collectionId, documentId));
        updateRequest.Content = new StringContent(updateBody, Encoding.UTF8, "application/json");
        using var updateResponse = await _httpClient.SendAsync(updateRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var updateResponseBody = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
        if (updateResponse.IsSuccessStatusCode)
        {
            return;
        }

        if (updateResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            throw new HttpRequestException($"Appwrite update {collectionId}/{documentId} failed ({(int)updateResponse.StatusCode}): {TrimError(updateResponseBody)}");
        }

        var createBody = JsonSerializer.Serialize(new { documentId, data }, JsonOptions);
        using var createRequest = CreateRequest(HttpMethod.Post, $"{CollectionUrl(collectionId)}/documents");
        createRequest.Content = new StringContent(createBody, Encoding.UTF8, "application/json");
        using var createResponse = await _httpClient.SendAsync(createRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var createResponseBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Appwrite create {collectionId}/{documentId} failed ({(int)createResponse.StatusCode}): {TrimError(createResponseBody)}");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Appwrite-Project", _options.ProjectId);
        request.Headers.Add("X-Appwrite-Key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private string DocumentsUrl(string collectionId, IEnumerable<string> queries)
    {
        var queryString = string.Join("&", queries.Select(query => $"queries[]={Uri.EscapeDataString(query)}"));
        return $"{CollectionUrl(collectionId)}/documents?{queryString}";
    }

    private string DocumentUrl(string collectionId, string documentId) =>
        $"{CollectionUrl(collectionId)}/documents/{Uri.EscapeDataString(documentId)}";

    private string CollectionUrl(string collectionId) =>
        $"{_options.Endpoint.TrimEnd('/')}/databases/{Uri.EscapeDataString(_options.DatabaseId)}/collections/{Uri.EscapeDataString(collectionId)}";

    private static string JsonQuery(string method, int value) =>
        JsonSerializer.Serialize(new { method, values = new[] { value } }, JsonOptions);

    private static string TrimError(string body) => body.Length <= 500 ? body : body[..500];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}

public sealed class AppwriteDocumentPage
{
    public AppwriteDocumentPage() { }

    public AppwriteDocumentPage(int total, IReadOnlyList<AppwriteDocument> documents)
    {
        Total = total;
        Documents = documents.ToList();
    }

    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("documents")] public List<AppwriteDocument> Documents { get; set; } = new();
}

public sealed class AppwriteDocument
{
    [JsonPropertyName("$id")] public string Id { get; set; } = string.Empty;

    // Appwrite REST يعيد attributes على مستوى المستند نفسه، وليس داخل data.
    // JsonExtensionData يلتقط الحقول الديناميكية مثل origin وserverBookingId.
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public Dictionary<string, JsonElement> Data
    {
        get
        {
            var data = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (Fields.TryGetValue("data", out var nested) && nested.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in nested.EnumerateObject())
                {
                    data[property.Name] = property.Value;
                }
            }

            foreach (var field in Fields)
            {
                if (!field.Key.StartsWith("$", StringComparison.Ordinal))
                {
                    data[field.Key] = field.Value;
                }
            }

            return data;
        }
    }
}
