# Introduction 
This POC is to build an AI Interview screen in JavaScript only. We can use either OpenAI or Claude Anthropic.

# Getting Started
This is a stand alone html/javascript page.  The files for this are as follows:
1. index.html: main HTML page.
2. client/src/App.jsx: all the work is done on this file.  It will handle the UI setup and all the JavaScript calls. Should break this into components if we ever adopt this.
3. Server.js: this was added only to handle claude API calls.  Running this local ran into anthropic had CORS issues.  If you want to run this. Must have run both client & server.
4. Running this, using node 22.13.1 (nvm).
    4.a. You must run "**npm install**" in both root **AND*** in client folders before running.
    4.b. Run run this locally, go to root directory and run "**npm run dev**".  This will run both server and client at the same time.
5. Before running, be sure to copy "**.env.example**" and rename it to .env  -- Be sure to get ApiKeys for both ChatGPT and Claude before running.

# Run and Testing
1. in Root directory, run "**npm run dev**"
2. Opening page should have a couple of fields.  Here's what they do:
    2.a. Purporse of the conversation.  This is the command or summary of what we want the AI to do.  Such as, "create a list of questions to do....."  
    2.b. Client or Project Name.  Put the client name so we can make sure the questions stay on task for this client.
    2.c. Model. selecting this will allow you use either OpenAI/ChatGPT or Claude/Anthropic
    2.d. click on **Start Interview** when ready.

# Changes needed for this
- This setup is not ideal. We should not input the command, client name and models.  We'll need to build this to handle those elements in the background.  Either by some setup in WBP or ETL scripts during onboarding, but that part should be hiddent to the user if we decide to push this out to production.

# Commands for testing
- If you look at the bottom of client/src/main.jsx, you will see some commands that are commented out.  I tend to use those for testing.
- Like the changes stated, we'll need to have the commands loaded with the page or something, we will not want the client to see that.
- OpenAI = is JS only, does not call Node express
- Clause / Anthropic = that calls the node express server.  

