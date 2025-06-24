using System.Collections.Generic; 

namespace INVISIO.Models
{

    public class AIAnalysisResult
    {
        public string InsightsSummary { get; set; }

        public string EntityTopicConnections { get; set; }

        public string Recommendations { get; set; }

        public List<TopicFrequency> TopicFrequency { get; set; }

        public List<EntityFrequency> EntityFrequency { get; set; }


        public List<TimeSeriesData> TimeSeries { get; set; }
    }


    public class TopicFrequency
    {
        public string Topic { get; set; } 
        public int Count { get; set; }    
    }


    public class EntityFrequency
    {
        public string Entity { get; set; } 
        public int Count { get; set; }    
    }

    public class TimeSeriesData
    {
        public string Date { get; set; }

        public string Topic { get; set; }

        public int Count { get; set; }
    }
}
