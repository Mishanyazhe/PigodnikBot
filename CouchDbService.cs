using MyCouch;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using MyCouch.Net;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Pigodnik
{
    public class CouchDbService
    {
        private readonly MyCouchClient _client;

        public CouchDbService(IConfiguration configuration)
        {
            string CouchDBUrl = configuration["UrlReferens:CouchDBUrl"];

            string userNameForCouchDB = configuration["CouchDBAutentification:UserName"];
            string passwordForCouchDB = configuration["CouchDBAutentification:Password"];

            var connectionInfo = new DbConnectionInfo(CouchDBUrl, configuration["CouchDBAutentification:databaseName"])
            {
                BasicAuth = new BasicAuthString(userNameForCouchDB, passwordForCouchDB)
            };

            _client = new MyCouchClient(connectionInfo);
        }

        public async Task SaveMessageAsync(long chatId, string messageType, string? text, string? filePath)
        {
            DateTime timestamp = DateTime.UtcNow;

            var document = new
            {
                ChatId = chatId,
                Type = messageType,
                Text = text,
                FilePath = filePath,
                Timestamp = timestamp
            };

            string jsonDocument = JsonSerializer.Serialize(document);

            var response = await _client.Documents.PostAsync(jsonDocument);

            if (!response.IsSuccess)
            {
                Console.WriteLine($"Ошибка при сохранении сообщения: {response.Reason}");
            }
        }

        public async Task<List<long>> GetUniqueChatIdsAsync(IConfiguration configuration)
        {
            try
            {
                var query = new
                {
                    selector = new
                    {
                        ChatId = new Dictionary<string, object>
                        {
                            ["$exists"] = true
                        }
                    },
                    fields = new[] { "ChatId" },
                    limit = 1000
                };

                string jsonQuery = JsonSerializer.Serialize(query);

                using var httpClient = new HttpClient();

                string CouchDBUrl = configuration["UrlReferens:CouchDBUrl"];
                string dbName = configuration["CouchDBAutentification:databaseName"];

                string userNameForCouchDB = configuration["CouchDBAutentification:UserName"];
                string passwordForCouchDB = configuration["CouchDBAutentification:Password"];

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes($"{userNameForCouchDB}:{passwordForCouchDB}")));

                var content = new StringContent(jsonQuery, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{CouchDBUrl}{dbName}/_find", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка выполнения запроса: {response.ReasonPhrase}");
                    return new List<long>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                var document = JsonDocument.Parse(responseContent);
                var chatIds = document.RootElement.GetProperty("docs")
                    .EnumerateArray()
                    .Select(doc => doc.GetProperty("ChatId").GetInt64())
                    .Distinct()
                    .ToList();

                return chatIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении уникальных ChatId: {ex.Message}");
                return new List<long>();
            }
        }
    }
}
