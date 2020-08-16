using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Web.Client
{
    public class SignalRadioClient
    {
        private readonly HttpClient _httpClient;
        private string _connectionString;

        public SignalRadioClient(string connectionString, HttpClient httpClient = null)
        {
            _connectionString = connectionString;

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                return true;
            };

            _httpClient = httpClient ?? new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(_connectionString);
        }
        
        public async Task<TalkGroup> GetTalkGroupByIdentifierAsync(ushort identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await _httpClient.GetAsync($"TalkGroups/Identifier/{identifier}", cancellationToken);
            return await response.Content.ReadAsAsync<TalkGroup>(cancellationToken);
        }
        public async Task<RadioCall> PostCallAsync(RadioCall radioCall, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await _httpClient.PostAsJsonAsync("RadioCalls", radioCall, cancellationToken);
            if(response.IsSuccessStatusCode)
                return await response.Content.ReadAsAsync<RadioCall>(cancellationToken);
            else
                throw new Exception("Add Error Handling...");
        }

        public async Task<TalkGroupImportResults> ImportTalkgroupCsvAsync(string talkGroupCsvPath)
        {
            var fileInfo = new FileInfo(talkGroupCsvPath);
            if(!fileInfo.Exists)
                throw new InvalidDataException();

            var streamContent = new StreamContent(File.OpenRead(fileInfo.FullName));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            var response = await _httpClient.PostAsync("TalkGroups/Import", streamContent);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<TalkGroupImportResults>();
        }
    }
}