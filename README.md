# SMS-to-IDE Server

A .NET 10 backend that captures project ideas via SMS and serves them as a structured, hierarchical API for the SMS-to-IDE VS Code Extension.

## 🛠️ Implementation Guide

### 1. Prerequisites

* A **Twilio** account (for the SMS webhook).
* A **VPS** (e.g., Linode) with Docker installed.
* The **SMS-to-IDE VS Code Extension**.

### 2. Deployment

The server is distributed as a Docker image. Use the following `docker-compose.yaml` to spin up your instance:

```yaml
services:
  sms-server:
    image: ghcr.io/yourappnerds/sms-to-ide-server:latest
    ports:
      - "5000:8080"
    environment:
      - ServerApiKey=your_secret_key          # Used to pair with VS Code
      - AllowedSenderPhone=+15550000000       # Only messages from this number are saved
      - TwilioAuthToken=your_twilio_token     # Required for Twilio signature validation
    volumes:
      - ./data:/app/data                      # Persists your SQLite database
    restart: unless-stopped
```

### 3. Twilio Configuration

Point your Twilio phone number's "Incoming Message" Webhook to:

https://your-vps-ip-or-domain:5000/sms (Method: HTTP POST).

### 4. SMS Syntax

- New Project: [MyProject] This is a new idea.
- Threaded Task: [42] Add a login button. (Where 42 is the ID of a previous message).