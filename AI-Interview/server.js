// server.js
import express from "express";
import cors from "cors";
import * as dotenv from "dotenv";
import Anthropic from "@anthropic-ai/sdk";

dotenv.config();

const app = express();

app.use(cors());
app.use(express.json());

const client = new Anthropic({ apiKey: process.env.CLAUDE_API_KEY });

app.get("/api/config", (req, res) => {
  res.json({
    message: "Server is working",
    hasClaudeKey: !!process.env.CLAUDE_API_KEY,
  });
});

app.post('/api/claude', async (req, res) => {
  const { messages } = req.body;

  try {
    const response = await client.messages.create({
      model: "claude-haiku-4-5",
      max_tokens: 1000,
      messages: messages.map(m => ({
        role: m.role === 'assistant' ? 'assistant' : 'user',
        content: m.content
      }))
    });

    res.json({ content: response.content[0].text });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: "Failed to contact Claude API: " + err.message });
  }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Proxy server running on http://localhost:${PORT}`);
});
