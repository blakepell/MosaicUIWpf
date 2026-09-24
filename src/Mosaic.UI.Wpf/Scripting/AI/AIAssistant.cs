/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Cysharp.Text;
using Microsoft.Extensions.AI;
using OllamaSharp;
// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Scripting.AI
{
    /// <summary>
    /// Represents an AI assistant that facilitates communication with an AI model through an ollama specified API endpoint.
    /// </summary>
    public class AIAssistant : IDisposable
    {
        /// <summary>
        /// Represents the chat client used for communication. This field may be null if the chat client has not been
        /// initialized.
        /// </summary>
        private IChatClient? _chatClient;

        /// <summary>
        /// Gets or sets the system prompt used to initialize the context or provide default instructions.
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Gets or sets the URL used to connect to the service.
        /// </summary>
        public string Url { get; set; } = "http://localhost:11434/";

        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        public string Model { get; set; } = "gemma3:12b";

        /// <summary>
        /// Gets or sets the history of chat messages, organized by user.
        /// </summary>
        public ConcurrentDictionary<string, List<ChatMessage>> History { get; set; } = new();

        /// <summary>
        /// Gets or sets a dictionary of change messages that maps to longer living user facts.
        /// </summary>
        public ConcurrentDictionary<string, List<ChatMessage>> Facts { get; set; } = new();

        /// <summary>
        /// Gets or sets a dictionary of state information that can be used to store arbitrary object that
        /// will be serialized and sent at the end of the system prompt.
        /// </summary>
        public ConcurrentDictionary<string, object> State { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AIAssistant"/> class.
        /// </summary>
        public AIAssistant()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AIAssistant"/> class with the specified API endpoint and model.
        /// </summary>
        /// <remarks>The constructor sets up the AI assistant by configuring the API client with the
        /// provided URL and model. Ensure that the URL points to a valid API endpoint and that the model name
        /// corresponds to a supported AI model.</remarks>
        /// <param name="url">The URL of the API endpoint to connect to. This cannot be null or empty.</param>
        /// <param name="model">The name of the AI model to use. This cannot be null or empty.</param>
        public AIAssistant(string url, string model)
        {
            this.Url = url;
            this.Model = model;
            _chatClient = new OllamaApiClient(this.Url, this.Model);
        }

        /// <summary>
        /// Sends a message and retrieves a response asynchronously.
        /// </summary>
        /// <param name="msg">The message to send. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response as a string,  or
        /// <see langword="null"/> if no response is available.</returns>
        public async Task<string?> AskAsync(string msg)
        {
            return await AskAsync(msg, "default");
        }

        /// <summary>
        /// Asks the AI assistant a question, maintaining conversation history per 'from' identifier.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="from"></param>
        public async Task<string?> AskAsync(string msg, string from)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            // Set the chat client up if it hasn't already been setup.
            _chatClient ??= new OllamaApiClient(this.Url, this.Model);

            // Get or create the history for this player
            var history = this.History.GetOrAdd(from, _ => new List<ChatMessage>());
            var userMessage = new ChatMessage(ChatRole.User, msg);

            history.Add(userMessage);

            using (var sb = ZString.CreateStringBuilder())
            {
                // Conversation is the whole of what is sent to the AI model.
                await foreach (var update in _chatClient.GetStreamingResponseAsync(this.Conversation(from), options: null))
                {
                    if (update.Text is { Length: > 0 })
                    {
                        sb.Append(update.Text);
                    }
                }

                var assistantMessage = new ChatMessage(ChatRole.Assistant, sb.ToString());
                history.Add(assistantMessage);

                return assistantMessage.Text;
            }
        }

        /// <summary>
        /// Updates the state associated with the specified user key.
        /// </summary>
        /// <remarks>If the specified key does not exist, a new entry is added to the state. If the key
        /// already exists, its value is replaced.</remarks>
        /// <param name="from">The key identifying the state to update. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="obj">The new state value to associate with the specified key. Can be <see langword="null"/>.</param>
        public void SetState(string from, object obj)
        {
            this.State[from] = obj;
        }

        /// <summary>
        /// Adds a fact associated with a specific source to the collection of facts.
        /// </summary>
        public void AddFact(string from, string fact)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            if (string.IsNullOrWhiteSpace(fact))
            {
                return;
            }

            var facts = this.Facts.GetOrAdd(from, _ => new List<ChatMessage>());
            facts.Add(new ChatMessage(ChatRole.User, fact));
        }

        /// <summary>
        /// Retrieves the conversation history and relevant facts for a specified user used to send to the AI model.
        /// </summary>
        /// <remarks>The first message in the returned sequence is always the system prompt. If no facts
        /// or history are associated with the specified user, only the system prompt is returned.</remarks>
        /// <param name="from">The identifier of the user whose conversation data is being retrieved. If null, empty, or whitespace, a
        /// default identifier is used.</param>
        /// <returns>An enumerable collection of <see cref="ChatMessage"/> objects representing the system prompt, followed by
        /// the user's facts and conversation history, if available.</returns>
        public IEnumerable<ChatMessage> Conversation(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            // Build a comprehensive system message
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.AppendLine(this.SystemPrompt ?? "You are a helpful assistant.");
                yield return new ChatMessage(ChatRole.System, sb.ToString());
            }

            // The actual conversation history between the AI assistant and the user.
            this.History.TryGetValue(from, out var history);

            if (history != null)
            {
                foreach (var m in history)
                {
                    yield return m;
                }
            }

            // Any facts that are longer living than the history.
            this.Facts.TryGetValue(from, out var facts);

            if (facts != null)
            {
                foreach (var m in facts)
                {
                    yield return m;
                }
            }

            // Add state information directly to the system prompt
            this.State.TryGetValue(from, out var state);

            if (state != null)
            {
                using (var sb = ZString.CreateStringBuilder())
                {
                    sb.AppendLine();
                    sb.AppendLine("CURRENT STATE INFORMATION:");

                    if (state is string stateString)
                    {
                        sb.AppendLine(stateString);
                    }
                    else
                    {
                        var stateJson = JsonSerializer.Serialize(state, new JsonSerializerOptions
                        {
                            WriteIndented = true // Better readability for AI
                        });

                        sb.AppendLine(stateJson);
                        sb.AppendLine("Always use the most recent state information provided above when it comes to those data points.");
                    }

                    yield return new ChatMessage(ChatRole.User, sb.ToString());
                }
            }
        }

        /// <summary>
        /// Saves the AI assistant's history and facts to a specified file asynchronously.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SaveAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            try
            {
                var data = new
                {
                    History = this.History.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Facts = this.Facts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save AI assistant data to '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Loads the AI assistant's history and facts from a specified file asynchronously.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task LoadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                return; // File doesn't exist, nothing to load
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return; // Empty file, nothing to load
                }

                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (data == null)
                {
                    return;
                }

                // Clear existing data
                this.History.Clear();
                this.Facts.Clear();

                // Load History
                if (data.TryGetValue("History", out var history))
                {
                    var historyElement = (JsonElement)history;
                    var historyDict = JsonSerializer.Deserialize<Dictionary<string, List<ChatMessage>>>(historyElement.GetRawText());

                    if (historyDict != null)
                    {
                        foreach (var kvp in historyDict)
                        {
                            this.History.TryAdd(kvp.Key, kvp.Value);
                        }
                    }
                }

                // Load Facts
                if (data.TryGetValue("Facts", out var facts))
                {
                    var factsElement = (JsonElement)facts;
                    var factsDict = JsonSerializer.Deserialize<Dictionary<string, List<ChatMessage>>>(factsElement.GetRawText());

                    if (factsDict != null)
                    {
                        foreach (var kvp in factsDict)
                        {
                            this.Facts.TryAdd(kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load AI assistant data from '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Clears all stored data, including history, facts, and state.
        /// </summary>
        /// <remarks>This method resets the internal state of the object by clearing history, facts, and
        /// any other stored information.  After calling this method, the object will be in its initial state.</remarks>
        public void Clear()
        {
            ClearHistory();
            ClearFacts();
            ClearState();
        }

        /// <summary>
        /// Clears all data associated with the specified from user.
        /// </summary>
        /// <remarks>This method removes all history, facts, and state related to the specified source. 
        /// Ensure that the provided identifier corresponds to a valid source to avoid unintended behavior.</remarks>
        /// <param name="from">The identifier of the source whose data should be cleared. Cannot be null or empty.</param>
        public void Clear(string from)
        {
            ClearHistory(from);
            ClearFacts(from);
            ClearState(from);
        }

        /// <summary>
        /// Clears all entries from the history.
        /// </summary>
        public void ClearHistory()
        {
            this.History.Clear();
        }

        /// <summary>
        /// Clears history for a specific 'from' identifier.
        /// </summary>
        /// <param name="from"></param>
        public void ClearHistory(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            this.History.TryRemove(from, out _);
        }

        /// <summary>
        /// Clears all entries from the facts history.
        /// </summary>
        public void ClearFacts()
        {
            this.Facts.Clear();
        }

        /// <summary>
        /// Clears facts for a specific 'from' identifier.
        /// </summary>
        /// <param name="from"></param>
        public void ClearFacts(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            this.Facts.TryRemove(from, out _);
        }


        /// <summary>
        /// Clears all entries from the state history.
        /// </summary>
        public void ClearState()
        {
            this.State.Clear();
        }

        /// <summary>
        /// Clears state for a specific 'from' identifier.
        /// </summary>
        /// <param name="from"></param>
        public void ClearState(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            this.State.TryRemove(from, out _);
        }

        /// <summary>
        /// Estimates the token usage for a specific 'from' identifier.
        /// </summary>
        /// <param name="from"></param>
        public int EstimatedTokenUsage(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                from = "default";
            }

            int total = 0;

            if (this.Facts.TryGetValue(from, out var facts))
            {
                foreach (var m in facts)
                {
                    total += EstimateTokenLength(m.Text);
                }
            }

            if (this.History.TryGetValue(from, out var history))
            {
                foreach (var m in history)
                {
                    total += EstimateTokenLength(m.Text);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.SystemPrompt))
            {
                total += EstimateTokenLength(this.SystemPrompt);
            }

            return total;
        }

        /// <summary>
        /// Estimates the token length of a message.
        /// </summary>
        /// <param name="msg"></param>
        public static int EstimateTokenLength(string msg)
        {
            return msg.Length / 4;
        }

        /// <summary>
        /// Cleans up any resources associated with the AI assistant.
        /// </summary>
        public void Dispose()
        {
            _chatClient?.Dispose();
        }
    }
}
