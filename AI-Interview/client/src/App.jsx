// App.jsx
import { useState } from 'react';

function App() {
  const [screen, setScreen] = useState('start');
  const [context, setContext] = useState({
    purpose: '',
    client: 'AI Test',
    model: 'openai'
  });
  const [messages, setMessages] = useState([]);
  const [answers, setAnswers] = useState([]);
  const [currentQuestion, setCurrentQuestion] = useState('');
  const [userResponse, setUserResponse] = useState('');
  const [warning, setWarning] = useState('');
  const [summary, setSummary] = useState('');
  const [conversationLog, setConversationLog] = useState([]);
  const [loading, setLoading] = useState(false);

  const claudApi = "xxxxx";
  const openAiApi = "zzzzz";
  
  const callOpenAI = async (apiKey, msgs) => {
    const response = await fetch("https://api.openai.com/v1/chat/completions", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${apiKey}`
      },
      body: JSON.stringify({
        model: "gpt-4-turbo",
        messages: msgs,
        temperature: 0.7
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error.message);
    }

    return await response.json();
  };

  const callClaude = async (apiKey, msgs) => {
    // Call your Express server proxy
    const response = await fetch("http://localhost:3000/api/claude", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ apiKey, messages: msgs })
    });

    if (!response.ok) {
      throw new Error('Failed to contact Claude API through proxy');
    }

    const data = await response.json();
    return {
      choices: [{ message: { content: data.content } }]
    };
  };

  const callLLM = async (apiKey, msgs) => {
    if (context.model === 'openai') {
      return await callOpenAI(apiKey, msgs);
    } else if (context.model === 'claude') {
      return await callClaude(apiKey, msgs);
    } else {
      throw new Error("Unknown model selected");
    }
  };

  const getNextQuestion = async (currentMessages) => {
    const apiKey = context.model === 'openai' ? openAiApi : claudApi;
    setLoading(true);
    
    try {
      const result = await callLLM(apiKey, currentMessages);

      if (!result || !result.choices || result.choices.length === 0) {
        throw new Error("No valid response from API.");
      }

      const aiMessage = result.choices[0].message.content?.trim();

      if (!aiMessage) {
        throw new Error("API returned an empty message.");
      }

      setConversationLog(prev => [...prev, { role: 'assistant', content: aiMessage }]);
      setCurrentQuestion(aiMessage);
      setMessages([...currentMessages, { role: 'assistant', content: aiMessage }]);
      setWarning('');
    } catch (err) {
      console.error("Error getting next question:", err);
      setCurrentQuestion("⚠️ Failed to get question. Try again later.");
      setWarning(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleStart = async () => {
    if (!context.purpose || !context.client) {
      alert("Please fill in all fields.");
      return;
    }

    const initialMessages = [
      {
        role: 'system',
        content: `You are conducting an interview to collect information for the purpose of: "${context.purpose}".
                The responses will be about the client or project: "${context.client}". 
                Ask detailed, thoughtful questions one at a time. Encourage elaboration. No one-word answers.`
      }
    ];

    setMessages(initialMessages);
    setScreen('chat');
    await getNextQuestion(initialMessages);
  };

  const handleSubmit = async () => {
    const input = userResponse.trim();
    if (input.split(/\s+/).length < 5) {
      setWarning("Please provide a longer answer (at least 5 words).");
      return;
    }

    setConversationLog(prev => [...prev, { role: 'user', content: input }]);
    const newAnswers = [...answers, input];
    setAnswers(newAnswers);
    
    const newMessages = [...messages, { role: 'user', content: input }];
    setMessages(newMessages);
    setUserResponse('');

    const totalLength = newAnswers.join(' ').length;
    if (totalLength >= 800) {
      await generateSummary(newMessages);
    } else {
      await getNextQuestion(newMessages);
    }
  };

  const generateSummary = async (currentMessages) => {
    const apiKey = context.model === 'openai' ? openAiApi : claudApi;
    setLoading(true);
    
    try {
      const summaryMessages = [
        ...currentMessages,
        {
          role: 'system',
          content: `Based only on the previous user answers, write a summary of their experience in a story that is first person perspective with ${context.client} for the purpose of ${context.purpose}. The summary must be **under 2000 characters** with proper punctuation and grammar. Avoid overly descriptive introductions and Focus on the speakers perspective.`
        }
      ];

      const result = await callLLM(apiKey, summaryMessages);
      const summaryText = result?.choices?.[0]?.message?.content || "(No summary generated)";

      setSummary(summaryText);
      setScreen('summary');
    } catch (err) {
      console.error("Error generating summary:", err);
      setWarning("Failed to generate summary: " + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif', maxWidth: '800px', margin: '0 auto' }}>
      <h1>AI Experience Interview</h1>
      
      {screen === 'start' && (
        <div>
          <p>
            <label><strong>Purpose of the conversation:</strong></label><br />
            <textarea
              value={context.purpose}
              onChange={(e) => setContext({ ...context, purpose: e.target.value })}
              rows="8"
              cols="40"
              placeholder="e.g., create a testimonial, gather case study info"
              style={{ width: '100%', marginTop: '5px', padding: '8px', fontSize: '14px' }}
            />
          </p>

          <p>
            <label style={{ marginTop: '1em' }}><strong>Client or Project Name:</strong></label>
            <input
              type="text"
              value={context.client}
              onChange={(e) => setContext({ ...context, client: e.target.value })}
              placeholder="e.g., ACME Corp Website Redesign"
              style={{ width: '100%', marginTop: '5px', padding: '8px', fontSize: '14px' }}
            />
          </p>

          <p>
            <label>Model:</label><br />
            <select
              value={context.model}
              onChange={(e) => setContext({ ...context, model: e.target.value })}
              style={{ marginTop: '5px', padding: '8px', fontSize: '14px' }}
            >
              <option value="openai">OpenAI (GPT-4-Turbo)</option>
              <option value="claude">Anthropic (claude-3-haiku)</option>
            </select>
          </p>

          <button 
            onClick={handleStart} 
            style={{ 
              marginTop: '1em', 
              padding: '10px 20px',
              fontSize: '16px',
              cursor: 'pointer',
              backgroundColor: '#007bff',
              color: 'white',
              border: 'none',
              borderRadius: '4px'
            }}
          >
            Start Interview
          </button>
          {warning && <div style={{ color: 'red', marginTop: '10px' }}>{warning}</div>}
        </div>
      )}

      {screen === 'chat' && (
        <div>
          <div style={{
            marginBottom: '1em',
            border: '1px solid #ccc',
            padding: '1em',
            maxHeight: '300px',
            overflowY: 'auto',
            background: '#f9f9f9',
            borderRadius: '4px'
          }}>
            {conversationLog.map((msg, idx) => (
              <div key={idx} style={{ marginBottom: '15px' }}>
                <strong>{msg.role === 'assistant' ? 'AI:' : 'You:'}</strong> {msg.content}
              </div>
            ))}
          </div>

          <div style={{ fontWeight: 'bold', marginBottom: '10px', fontSize: '16px' }}>
            {currentQuestion}
          </div>
          
          <textarea
            value={userResponse}
            onChange={(e) => setUserResponse(e.target.value)}
            onKeyPress={handleKeyPress}
            rows="5"
            placeholder="Please elaborate..."
            disabled={loading}
            style={{ 
              width: '100%', 
              marginBottom: '10px',
              padding: '8px',
              fontSize: '14px',
              borderRadius: '4px',
              border: '1px solid #ccc'
            }}
          />
          <br />
          <button 
            onClick={handleSubmit}
            disabled={loading}
            style={{ 
              padding: '10px 20px',
              fontSize: '16px',
              cursor: loading ? 'not-allowed' : 'pointer',
              backgroundColor: loading ? '#ccc' : '#28a745',
              color: 'white',
              border: 'none',
              borderRadius: '4px'
            }}
          >
            {loading ? 'Processing...' : 'Submit'}
          </button>
          {warning && <div style={{ color: 'red', marginTop: '10px' }}>{warning}</div>}
        </div>
      )}

      {screen === 'summary' && (
        <div>
          <div style={{
            marginBottom: '1em',
            border: '1px solid #ccc',
            padding: '1em',
            maxHeight: '300px',
            overflowY: 'auto',
            background: '#f9f9f9',
            borderRadius: '4px'
          }}>
            {conversationLog.map((msg, idx) => (
              <div key={idx} style={{ marginBottom: '15px' }}>
                <strong>{msg.role === 'assistant' ? 'AI:' : 'You:'}</strong> {msg.content}
              </div>
            ))}
          </div>

          <hr />
          <h3>Summary for {context.client}</h3>
          <p style={{
            whiteSpace: 'pre-wrap',
            background: '#f1f1f1',
            padding: '1em',
            borderRadius: '5px',
            lineHeight: '1.6'
          }}>
            {summary}
          </p>
        </div>
      )}
    </div>
  );
}

export default App;