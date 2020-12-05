using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Hara.Abstractions.Contracts;

namespace Hara.Abstractions.Services
{
    public class WeatherForecastFetcher : IWeatherForecastFetcher
    {
        private readonly ILocalContentFetcher _localContentFetcher;

        public WeatherForecastFetcher(ILocalContentFetcher localContentFetcher)
        {
            _localContentFetcher = localContentFetcher;
        }
        public async Task<IEnumerable<WeatherForecast>> Fetch()
        {
            var content = await _localContentFetcher.Fetch("_content/Hara.UI/weather.json");

            if (content != null)
            {
                return await JsonSerializer.DeserializeAsync<WeatherForecast[]>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            }

            return null;
        }
    }
}