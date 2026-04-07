Fin-Insight AI: Natural Language Financial Data Analyst
Fin-Insight AI is a GenAI-powered financial assistant built with .NET 10. It allows users to query a PostgreSQL database using natural language. The system automatically translates human questions into precise SQL queries, executes them, and provides a professional financial summary of the results.

🚀 Features
Text-to-SQL Engine: Powered by Google Gemini 2.0/2.5 Flash.

Agentic AI Workflow: A multi-step process that generates logic, executes data retrieval, and interprets results.

Containerized Infrastructure: Fully automated database setup using Docker Compose.

Clean Architecture: Utilizes Dependency Injection and Dapper for lightweight, high-performance data access.

🛠️ Tech Stack

Runtime: .NET 10 


AI Engine: Google Gemini API (Mscc.GenerativeAI SDK) 

Database: PostgreSQL 16 (Running in Docker)


ORM: Dapper 

Infrastructure: Docker & Docker Compose

📖 Setup Guide
Follow these steps to replicate the project on your local machine.

1. Prerequisites
.NET 10 SDK installed.

Docker Desktop installed.

A Gemini API Key from Google AI Studio.

2. Create the Project and Install Dependencies
Open your terminal and run the following commands:
# Create a new Web API project
dotnet new webapi -n FinInsightAI
cd FinInsightAI

# Install required NuGet packages
dotnet add package Dapper
dotnet add package Npgsql
dotnet add package Mscc.GenerativeAI
dotnet add package Microsoft.AspNetCore.OpenApi

3. Configure Infrastructure (Docker)
Create a docker-compose.yml file in your project root to set up PostgreSQL and pgAdmin:
version: '3.8'
services:
  postgres:
    image: postgres:16
    container_name: fininsight_postgres
    environment:
      POSTGRES_USER: myuser
      POSTGRES_PASSWORD: mypassword
      POSTGRES_DB: FinInsightDb
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
  pgadmin:
    image: dpage/pgadmin4
    container_name: fininsight_pgadmin
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@admin.com
      PGADMIN_DEFAULT_PASSWORD: adminpassword
    ports:
      - "5050:80"
volumes:
  postgres_data:

4. Database Initialization
Create an init.sql file in the same directory. Docker will automatically run this to create your table and seed data:

CREATE TABLE IF NOT EXISTS loans (
    id SERIAL PRIMARY KEY,
    customer_name VARCHAR(100),
    amount DECIMAL,
    status VARCHAR(20),
    loan_date DATE
);

INSERT INTO loans (customer_name, amount, status, loan_date)
VALUES
    ('Alice Smith', 500000, 'Approved', '2024-03-01'),
    ('Bob Jones', 1200000, 'Pending', '2024-03-15'),
    ('Charlie Brown', 800000, 'Approved', '2024-03-20');


5. Application Configuration
Update your appsettings.json with your Gemini API Key:
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE"
  }
}

6. Run the Application
- Start the Database:
    - docker-compose up -d

- Start the .NET API:
    - dotnet run

🎮 How to Use
The API provides a single endpoint: GET /ask?prompt={your_question}.

Example Request:
http://localhost:5091/ask?prompt=Find customers whose name starts with Smith

Example Response:

{
  "question": "Find customers whose name starts with Smith",
  "sql": "SELECT * FROM loans WHERE customer_name LIKE 'Smith%';",
  "data": [
    {
      "id": 1,
      "customer_name": "Alice Smith",
      "amount": 500000,
      "status": "Approved",
      "loan_date": "2024-03-01"
    }
  ],
  "aiAnalysis": "I found one customer matching your request: Alice Smith. She has an approved loan of 500,000 USD issued on March 1st, 2024."
}

🧠 Key Logic: The Agentic Loop
- The project follows a 3-step AI logic within the /ask endpoint: 
- Generation: Gemini receives the database schema and the user's question to generate a valid PostgreSQL query. 
- Execution: The system uses Dapper to execute the raw SQL against the Postgres container safely. 
- Summarization: The raw JSON data result is sent back to Gemini to be transformed into a professional, human-readable insight.


Subsequent Cloud Deployment Four Steps:
1. Local End: Packaging and Pushing (Build & Push)
Source: Docker grabs the code and Dockerfile from your local computer folder. 
- Action: You run docker build on your computer, packaging the code into an Image. 
- Transfer: You run docker push to send this Image to AWS ECR (cloud repository). 
    - Note: EC2 won't directly grab files from GitHub or your computer; it picks up from the ECR repository.

2. EC2 Preparation: Authentication (Auth)
- Login: You run aws configure on EC2 and input Access Key. 
    - Purpose: This step is to give EC2 'entry permission' so it has the authority to pull your private Image from the ECR repository.
- Create RDS: Launch a PostgreSQL instance in AWS Console and note the Endpoint.

3. Database Initialization (Database Schema & Data)
- Connection: Connect from local pgAdmin to cloud RDS via Endpoint. 
- Initialization: Run init.sql to create the loans table and insert test data (Li Ahua, Zhang Xiaoming). 
- Significance: Ensure that when the API container starts, the backend has 'fresh data' to query, avoiding 500 Error.

4. Deployment: Pull and Run (Pull & Run)
- Pickup: Run docker pull, EC2 downloads the Image you just uploaded from ECR. 
- Start: Run docker run, this is when the container is actually created. 
- Injection: You pass the Gemini Key and RDS Endpoint connection string via -e in the command, so the program knows how to communicate with AI and the database.

5. Infrastructure: Opening Doors and Connections (Network)
- External: Open Port 80 in Security Group so your browser can access EC2. 
- Internal: Confirm RDS firewall allows EC2 to access Port 5432. 
- Initialization: Run init.sql in RDS to create the loans table.
    - Test: Visit http://EC2-IP/ask in browser, confirm AI can successfully retrieve and analyze data from RDS.

Summary of AWS Achievements:
It's not simply 'starting a computer', but completing a 'distributed architecture': 
- Compute Layer (Compute): EC2 + Docker Container. 
- Data Layer (Storage): AWS RDS (PostgreSQL). 
- Intelligence Layer (AI): Google Gemini API. 
- Repository Layer (Registry): AWS ECR.
