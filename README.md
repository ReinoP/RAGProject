# Project

This project is a full-stack web application that demonstrates a Retrieval-Augmented Generation (RAG) pipeline using local LLM models via Ollama.

Users can upload text documents and query their content using natural language. 
The system processes documents by splitting them into chunks, generating embeddings, and storing them for efficient semantic retrieval.

When a user asks a question, the application retrieves the most relevant document chunks using cosine similarity 
and provides them as context to a local LLM (Llama), which generates the final answer.

Originally I intented to make a more multifaceted project with many different LLMs, with orchestrators and different services,
but at the end I decided to focus on RAG, and possibly use what I learned in another project instead.

Because this was my first small hands-on project with LLMs:
I used SQLite instead of an actual vector database and embeddings are stored as JSON strings to keep it simple, naturally for production this would not suffice.
Used local LLMs for gaining some more experience, and they were free, flexible and enough for my purposes with this project.


## How to setup SQLite after cloning the repo
in powershell type in these commands:
Add-Migration InitialCreate
Update-Database

## Setup Ollama
Download Ollama https://ollama.com
in cmd-prompt you can then "ollama pull embeddinggemma" and "ollama pull llama3" to download the required LLMs
NOTE that Ollama service will run on your pc startup automatically unless you configure otherwise. 
You should also disable cloud models, auto-download uploads, and "Expose Ollama to the network" settings.
You can change context length as you wish, for me 4k was enough for my small tests.

And you should be ready to go!

## There is a test_data.txt in /Docs folder for quick uploading and testing.


---

### Tech Stack

- **Backend:** .NET Core MVC  
- **Frontend:** Vue.js (Options API)  
- **AI Models:** Ollama ->  `embeddinggemma` (latest), `llama` (latest)  
- **Caching:** In-memory caching
- **SQLite:** Simple and light-weight database
- **TailWind:** For CSS
- **Google Stitch:** Used for basic UI layout
---

### Features

- AI-powered embeddings and responses using Ollama models  
- Simple caching system for improving response times  
- MVC pattern for clean separation of concerns  
- Vue Options API for reactive and maintainable frontend components  
---

![RAG demo upload](Docs/RAGUpload.png)
![RAG demo questions](Docs/RAGQuestions.png)
*Simple UI showing document upload and question-answer interface*