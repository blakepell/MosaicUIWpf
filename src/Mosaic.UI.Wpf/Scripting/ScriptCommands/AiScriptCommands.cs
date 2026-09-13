/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Scripting;
using System.Threading.Tasks;
using Mosaic.UI.Wpf.Scripting.AI;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Commands to interact with AI cloud services.
    /// </summary>
    [ScriptModule(Name = "ai", Description = "Commands to interact with AI cloud services.")]
    public partial class AiScriptCommands
    {
        private AIAssistant _ai;

        /// <summary>
        /// Constructor
        /// </summary>
        public AiScriptCommands()
        {
            _ai = new AIAssistant();
            this.SetModel("gemma3:12b");
        }

        /// <summary>
        /// (async/await) Asks a question to the a local model via ollama.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(AskAsync),
            Description = "(async/await) Asks a question to the a local model via ollama.",
            ParameterCount = 2)]
        public async Task<string> AskAsync(string msg)
        {
            // Ensure a non-null string is always returned
            return await _ai.AskAsync(msg) ?? string.Empty;
        }

        /// <summary>
        /// (async/await) Asks a question to the a local model via ollama.  This tethers the request to a specific user where the model will refer to it&apos;s conversation with that user in context.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(AskAsync),
            Description = "(async/await) Asks a question to the a local model via ollama.  This tethers the request to a specific user where the model will refer to it's conversation with that user in context.",
            ParameterCount = 2)]
        public async Task<string> AskAsync(string msg, string fromUser)
        {
            // Ensure a non-null string is always returned
            return await _ai.AskAsync(msg, fromUser) ?? string.Empty;
        }

        /// <summary>
        /// Sets the model that you are currently running in ollama.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(SetModel),
            Description = "Sets the model that you are currently running in ollama.",
            ParameterCount = 1)]
        public void SetModel(string modelName)
        {
            _ai.Model = modelName;
        }

        /// <summary>
        /// Sets the system prompt which is the most important instructions on how the AI model should behave.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(SetModel),
            Description = "Sets the system prompt which is the most important instructions on how the AI model should behave.",
            ParameterCount = 1)]
        public void SetSystemPrompt(string systemPrompt)
        {
            _ai.SystemPrompt = systemPrompt;
        }

        /// <summary>
        /// Sets a state information object onto the specified conversation.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(SetModel),
            Description = "Sets a state information object onto the specified conversation.",
            ParameterCount = 2)]
        public void SetState(string from, object obj)
        {
            _ai.SetState(from, obj);
        }

        /// <summary>
        /// Clears the conversation history but preserves any facts or state.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(ClearHistory),
            Description = "Clears the conversation history but preserves any facts or state.",
            ParameterCount = 0)]
        public void ClearHistory()
        {
            _ai.ClearHistory();
        }

        /// <summary>
        /// Clears the conversation history, facts and any state that exists.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(SetModel),
            Description = "Clears the conversation history, facts and any state that exists.",
            ParameterCount = 0)]
        public void ClearAll()
        {
            _ai.Clear();
        }
    }
}