using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LSR.src.tools {
    public class RiotAccountService {
        private readonly string api_key;
        private readonly HttpClient httpClient;

        public RiotAccountService(string key) {
            api_key = key;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Riot-Token", key);
        }
        /// <summary>
        /// Queries the Player's Universally Unique Identification.
        /// </summary>
        /// <param name="region">E.g. Americas</param>
        /// <param name="playerName">Their in-game name</param>
        /// <param name="tagline">Their associated hashtag (#)</param>
        /// <returns>PUUID as string</returns>
        public async Task<string> GetPUUID(string region, string playerName, string tagline) {
            string encodedName      = Uri.EscapeDataString(playerName);
            string encodedTagline   = Uri.EscapeDataString(tagline);

            string url = $"https://{region.ToLower()}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{encodedName}/{encodedTagline}";

            try {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode) {
                    string jsonString = await response.Content.ReadAsStringAsync();

                    RiotAccountDTO account = JsonConvert.DeserializeObject<RiotAccountDTO>(jsonString);
                    return account.PUUID;
                }
                else {
                    string error = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Status: {(int)response.StatusCode}");

                    return $"e1 {response.StatusCode}: {error}";
                }
            }
            catch (Exception ex) {
                // append the e1 error code for quick identification of failed connection
                Console.WriteLine($"BRUH: {ex.Message}");
                return "e2" + ex.Message;
            }
        }
    }

    // data transfer object to hold received account information
    internal class RiotAccountDTO {
        [JsonProperty("puuid")]
        public string PUUID { get; set; }

        [JsonProperty("gameName")]
        public string GameName { get; set; }

        [JsonProperty("tagLine")]
        public string TagLine { get; set; }
    }
}
