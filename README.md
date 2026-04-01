# Project

A simple web application for Retrieval-augmented generation (RAG) using **.NET Core MVC** for the backend, **Vue.js (Options API)** for the frontend, and **Ollama** for AI-powered functionalities with the latest `embeddinggemma` and `llama` models.
User can upload text documents, then they are split into smaller chunks and embedded with embeddinggemma. These embeddings are stored in SQLite. 
When a question is asked, the system finds the most relevant chunks using cosine similarity, then the LLM (Llama) produces an answer using that context.
---

## Table of Contents

- [Tech Stack](#tech-stack)  
- [Features](#features)  

---

## Tech Stack

- **Backend:** .NET Core MVC  
- **Frontend:** Vue.js (Options API)  
- **AI Models:** Ollama ->  `embeddinggemma` (latest), `llama` (latest)  
- **Caching:** In-memory caching via `IMemoryCache`  
- **SQLite:** Simple and light-weight databasse
---

## Features

- AI-powered embeddings and responses using Ollama models  
- Simple caching system for improving response times  
- MVC pattern for clean separation of concerns  
- Vue Options API for reactive and maintainable frontend components  
---
