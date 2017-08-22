using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public class PollResults
    {
        public int TotalVotes { get; private set; }
        public IEnumerable<KeyValuePair<string, int>> Results { get; private set; }


        public PollResults(IEnumerable<KeyValuePair<string, int>> results)
        {
            Results = results;
            TotalVotes = results.Sum(x => x.Value);
        }


        public string GetResultsSortedString()
        {
            StringBuilder resultsBuilder = new StringBuilder();
            foreach (var pair in Results.OrderByDescending(x => x.Value))
            {
                if (resultsBuilder.Length > 0)
                    resultsBuilder.Append(", ");

                resultsBuilder.Append(pair.Key);
                resultsBuilder.Append(" - ");
                resultsBuilder.Append(pair.Value);
            }

            return resultsBuilder.ToString();
        }
    }
}
