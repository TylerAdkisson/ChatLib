using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            Announcement,
        };

        private List<TextRun> _runs;


        /// <summary>
        /// Gets or sets the author of the chat message
        /// </summary>
        public ChatterInfo Author { get; set; }

        /// <summary>
        /// Gets or set the timestamp of the chat message
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the kind of message (i.e. normal, action, etc.)
        /// </summary>
        public Kind MessageKind { get; set; }

        /// <summary>
        /// Gets the runs used in the message body
        /// </summary>
        public ReadOnlyCollection<TextRun> TextRuns { get { return _runs.AsReadOnly(); } }

        /// <summary>
        /// Gets or sets the message's identifier
        /// </summary>
        public string Id { get; set; }


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


        /// <summary>
        /// Appends the specified run to the message body
        /// </summary>
        /// <param name="run">The run to append</param>
        public void AppendRun(TextRun run)
        {
            if (run == null)
                throw new ArgumentNullException("run");

            _runs.Add(run);
        }

        /// <summary>
        /// Appends the specified series of runs to the message body
        /// </summary>
        /// <param name="runs">The runs to append</param>
        public void AppendRuns(IEnumerable<TextRun> runs)
        {
            if (runs == null)
                throw new ArgumentNullException("runs");

            _runs.AddRange(runs);
        
        }

        /// <summary>
        /// Removes all runs from the message body
        /// </summary>
        public void ClearRuns()
        {
            _runs.Clear();
        }

        /// <summary>
        /// Composes all text content of the body into a string
        /// </summary>
        /// <returns>A string containing all text content of the body</returns>
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
