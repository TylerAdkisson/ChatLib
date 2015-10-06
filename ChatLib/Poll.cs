using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChatLib
{
    public class Poll
    {
        public const string OptionsToken = "{options}";
        public const string WinnerCountToken = "{winTotal}";
        public const string TotalVotesToken = "{totalVotes}";
        public const string WinnerOptionToken = "{winOption}";
        public const string PollResultsToken = "{results}";
        public const string PollDurationToken = "{time}";


        private static Dictionary<string, Poll> _polls;

        private IChatChannel _channel;
        private Dictionary<string, int> _tally;
        private HashSet<string> _votedUsers;
        private Timer _votingTimer;
        private string _identifier;


        public TimeSpan VotingTimeLimit { get; set; }
        public bool AnnouncePoll { get; set; }
        public string AnnounceStartMessage { get; set; }
        public string AnnounceEndMessage { get; set; }

        public PollResults Results { get; private set; }


        public event EventHandler OnPollStart;
        public event PollResultsEventHandler OnPollProgress;
        public event PollResultsEventHandler OnPollFinish;


        public Poll(IChatChannel channel)
        {
            _channel = channel;
            _tally = new Dictionary<string, int>();
            _votingTimer = new Timer(VotingStop);
            _votedUsers = new HashSet<string>();
        }


        public static Poll Create(IChatChannel channel, string pollId)
        {
            if (_polls == null)
                _polls = new Dictionary<string, Poll>();

            if (_polls.ContainsKey(pollId))
                throw new ArgumentException("A poll with that ID already exists.");

            return _polls[pollId] = new Poll(channel) { _identifier = pollId };
        }

        public static Poll GetPoll(string pollId)
        {
            Poll poll;
            if (_polls == null || !_polls.TryGetValue(pollId, out poll))
                throw new ArgumentException("The specified poll does not exist.");

            return poll;
        }
        

        public void AddOption(string optionToken)
        {
            if (_tally.ContainsKey(optionToken))
                throw new InvalidOperationException("Token already exists");

            _tally[optionToken] = 0;
        }

        public void Start()
        {
            _votedUsers.Clear();

            // Raise poll start event
            RaiseOnPollStart();

            _channel.OnChatMessage += channel_OnChatMessage;

            if (AnnouncePoll && !string.IsNullOrEmpty(AnnounceStartMessage))
            {
                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.Append(AnnounceStartMessage);

                if (VotingTimeLimit != TimeSpan.Zero)
                {
                    messageBuilder.Replace(PollDurationToken, ((int)VotingTimeLimit.TotalSeconds).ToString());
                }

                string[] keys = new string[_tally.Keys.Count];
                _tally.Keys.CopyTo(keys, 0);
                string options = string.Join(" | ", keys);

                if (AnnounceStartMessage.IndexOf(OptionsToken) > -1)
                {
                    // Replace options token inside message
                    messageBuilder.Replace(OptionsToken, options);
                }
                else
                {
                    // Append options to message
                    messageBuilder.Append(options);
                }

                _channel.SendMessage(messageBuilder.ToString());
            }

            if (VotingTimeLimit != TimeSpan.Zero)
            {
                _votingTimer.Change(VotingTimeLimit, TimeSpan.FromMilliseconds(-1));
            }
        }

        public void Stop()
        {
            _channel.OnChatMessage -= channel_OnChatMessage;

            Results = new PollResults(_tally);
            RaiseOnPollFinish(Results);

            if (AnnouncePoll && !string.IsNullOrEmpty(AnnounceEndMessage))
            {
                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.Append(AnnounceEndMessage);

                if (AnnounceEndMessage.IndexOf(TotalVotesToken) > -1)
                {
                    messageBuilder.Replace(TotalVotesToken, Results.TotalVotes.ToString());
                }

                // The results of the poll are: Kappa - 30, PogChamp - 27, BibleThump - 19
                // AnnounceMessage {results}

                StringBuilder resultsBuilder = new StringBuilder();
                foreach (var pair in _tally.OrderByDescending(x => x.Value))
                {
                    if (resultsBuilder.Length > 0)
                        resultsBuilder.Append(" | ");

                    resultsBuilder.Append(pair.Key);
                    resultsBuilder.Append(" - ");
                    resultsBuilder.Append(pair.Value);
                }

                // Put the results into the result string
                if (AnnounceEndMessage.IndexOf(PollResultsToken) > -1)
                {
                    messageBuilder.Replace(PollResultsToken, resultsBuilder.ToString());
                }
                else
                {
                    messageBuilder.Append(resultsBuilder.ToString());
                }

                _channel.SendMessage(messageBuilder.ToString());
            }
        }

        public void Dispose()
        {
            OnPollStart = null;
            OnPollProgress = null;
            OnPollFinish = null;

            Remove(_identifier);
        }


        private static void Remove(string pollId)
        {
            if (_polls == null)
                return;

            _polls.Remove(pollId);
        }

        private void channel_OnChatMessage(object sender, ChatMessage message)
        {
            string chatterName = message.Author.Name.Text.ToLowerInvariant();
            if (false)
            {
                if (_votedUsers.Contains(chatterName))
                    return; // Already voted
            }

            if (true)
            {
                string token = message.ToString();
                if (_tally.ContainsKey(token))
                {
                    _tally[token]++;
                    _votedUsers.Add(chatterName);

                    RaiseOnPollProgress(new PollResults(_tally));
                }
            }
            else
            {
                // Match voting tokens anywhere in a message
                foreach (var run in message.TextRuns)
                {
                    // Split message segment into tokens at space
                    string[] tokens = run.Text.Split(' ');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (_tally.ContainsKey(tokens[i]))
                        {
                            _tally[tokens[i]]++;
                            _votedUsers.Add(chatterName);

                            RaiseOnPollProgress(new PollResults(_tally));

                            return;
                        }
                    }
                }
            }
        }

        private void VotingStop(object state)
        {
            Stop();
        }

        private void RaiseOnPollStart()
        {
            EventHandler handler = OnPollStart;
            if (handler == null)
                return;

            handler(this, null);
        }

        private void RaiseOnPollProgress(PollResults results)
        {
            PollResultsEventHandler handler = OnPollProgress;
            if (handler == null)
                return;

            handler(this, results);
        }

        private void RaiseOnPollFinish(PollResults results)
        {
            PollResultsEventHandler handler = OnPollFinish;
            if (handler == null)
                return;

            handler(this, results);
        }
    }
}
