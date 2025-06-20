using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace News.Core.Dtos.NewsCatcher
{
    public class RecommendationResponse
    {
        [JsonPropertyName("recommendations")]
        public List<NewsArticle> Recommendations { get; set; } = new();

    }
}
