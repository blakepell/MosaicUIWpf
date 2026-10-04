# ai — local model conversations

[Table of contents](README.md)

Use `ai` to send text to a local Ollama model and receive a complete response. The supplied module connects to `http://localhost:11434/` and initially selects `gemma3:12b`. Ollama must be running and the selected model must be available. These are Mosaic's defaults; an application can replace the module.

## Reference

| Call | Result | Behavior |
| --- | --- | --- |
| `await ai.AskAsync(msg)` | String | Ask in the conversation named `"default"`. |
| `await ai.AskAsync(msg, fromUser)` | String | Ask in the conversation identified by `fromUser`. |
| `ai.SetModel(modelName)` | None | Select the model for initial client creation; call before the first question. |
| `ai.SetSystemPrompt(systemPrompt)` | None | Set instructions included with subsequent questions. |
| `ai.SetState(from, obj)` | None | Replace contextual state for the given conversation key. Strings are supplied as text; other objects are serialized as JSON. |
| `ai.ClearHistory()` | None | Clear conversation messages for all users; retain state and facts. |
| `ai.ClearAll()` | None | Clear conversation messages, facts, and state for all users. |

All arguments except `obj` are strings. `AskAsync` returns `""` for an empty/whitespace question or when there is no response text. Empty/whitespace user identifiers passed to `AskAsync` select `"default"`. With `SetState`, pass the exact conversation key explicitly, such as `"default"`.

## Ask a question

The example sends the prompt to the configured local service when run:

```javascript
try {
    let answer = await ai.AskAsync("Explain what a checksum is in two sentences.");
    ui.ShowString(answer);
} catch (error) {
    log.Error(error.Message);
}
```

Always await `AskAsync` when you need the response. It returns one completed string, not streamed chunks in the script. Connection failures and model errors can throw exceptions.

## Keep a named conversation

Use a stable key to keep questions in the same conversation. The key is a label, not a login or authentication identity. In this example, both questions include the previous messages for `"report-helper"`:

```javascript
ai.SetSystemPrompt("Answer briefly and use plain language.");
let first = await ai.AskAsync("I am preparing a monthly sales report. Suggest three sections.", "report-helper");
ui.ShowString(first);

let followUp = await ai.AskAsync("Suggest a title for the first section.", "report-helper");
ui.ShowString(followUp);
```

Conversation history and settings live on the supplied `ai` object and can carry over to later script runs. Editors sharing that object share the same conversation state. Clearing history or all data affects every conversation on it, not just the latest named key.

## Supply current context

`SetState` replaces the state for its key and supplies it with subsequent questions. It does not send a request by itself. A string is useful when the desired context is already formatted:

```javascript
ai.SetState("report-helper", "Current period: September. Orders: 120. Returns: 4.");
let answer = await ai.AskAsync("Summarize the current period in one sentence.", "report-helper");
ui.ShowString(answer);
```

## Model and reset behavior

Call `ai.SetModel("gemma3:12b")`, or another installed model name, before the first nonempty question. The current implementation creates its client on first use; changing the name afterward does not rebuild that client. `ClearHistory` and `ClearAll` also leave the existing client, model selection, and system prompt in place.

There is no command here to change the service address, clear a single user's history, or cancel an in-flight request. Any facts supplied by the application survive `ClearHistory` but are removed by `ClearAll`. Context, prompts, and conversation history are sent to the configured model service when asking a question.
