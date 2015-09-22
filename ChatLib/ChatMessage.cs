using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib
{
    public class ChatMessage
    {
        public enum Kind
        {
            Normal,
            Action,
        };

        private List<TextRun> _runs;


        public TextRun Author { get; set; }
        public DateTime Timestamp { get; set; }
        public Kind MessageKind { get; set; }
        public IEnumerable<TextRun> TextRuns { get { return _runs; } }


        public ChatMessage()
        {
            _runs = new List<TextRun>();
        }

        public ChatMessage(IEnumerable<TextRun> runs)
        {
            _runs = new List<TextRun>();

            if (runs == null)
                throw new ArgumentNullException("runs");

            _runs.AddRange(runs);
        }


        public void AppendRun(TextRun run)
        {
            if (run == null)
                throw new ArgumentNullException("run");

            _runs.Add(run);
        }

        public void AppendRuns(IEnumerable<TextRun> runs)
        {
            if (runs == null)
                throw new ArgumentNullException("runs");

            _runs.AddRange(runs);
        
        }

        public void ClearRuns()
        {
            _runs.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in TextRuns)
            {
                sb.Append(item.Text);
            }

            return sb.ToString();
        }
    }
}
