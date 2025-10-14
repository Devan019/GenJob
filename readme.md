# GenJob — AI-assisted Job & Resume Toolkit

This repository contains two main components: an `ai-apis` microservice that provides AI-powered resume generation, salary prediction, and job analysis utilities; and `GenJobMVC`, an ASP.NET MVC web application that integrates those AI services into a web UI for employers and job seekers.

## Contents
- `ai-apis/` — Python FastAPI microservice providing AI helpers (resume LaTeX generation, salary prediction, job analysis, PDF conversion helper).
- `GenJobMVC/` — ASP.NET MVC web app (C#) that uses the microservice and provides UI pages (Dashboard, ATS, Resume generation, Salary prediction).

## Project features

- **AI-powered Resume Generation**: Generate professional LaTeX-based resumes from structured candidate data and job descriptions using AI models (oss-120b).
- **PDF Conversion**: Convert generated LaTeX to downloadable PDF URLs via latexonline.cc.
- **ATS Score Analysis**: Extract text from PDF/DOC resumes using iTextSharp/OpenXML and calculate ATS compatibility scores.
- **Salary Prediction**: ML-based salary prediction for job roles by company, location, and employment status with Redis caching.
- **Job Market Analytics**: Generate graph data and visualizations for job market trends and analysis.
- **User Authentication**: Secure login/signup system using ASP.NET Identity with MySQL database.
- **Job Search Integration**: Search job postings by role, location, and company via Glassdoor API integration.
- **Data Caching**: Redis-based caching for improved performance and data storage.
- **Resume Parsing**: Extract and process resume content from various document formats.

## Tech stack

**Libaries/Frameworks**
- Frontend: HTML, Tailwind-CSS, JavaScript, Razor views
- Backend: C#, .net core and it's packages
- AI/ML : python, fastapis

**ai-apis (Python Backend):**
- Python 3.10+
- FastAPI for REST API endpoints
- Pydantic for request/response validation
- Redis for caching and data storage
- scikit-learn for ML model inference
- Pre-trained ML models (salary prediction)
- oss-120b model for AI-powered content generation
- latexonline.cc for LaTeX → PDF compilation

**GenJobMVC (C# Web Application):**
- .NET 8.0 (ASP.NET Core MVC)
- Entity Framework Core for ORM and migrations
- MySQL database for persistent data storage
- ASP.NET Identity for authentication and user management
- iTextSharp for PDF text extraction
- OpenXML for document processing
- Redis.OM for Redis integration in .NET
- Razor views for server-side rendering
- Glassdoor API integration for job search

**Key .NET Packages:**
- `Microsoft.EntityFrameworkCore`
- `Pomelo.EntityFrameworkCore.MySql`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Redis.OM`
- `iTextSharp`
- `DocumentFormat.OpenXml`

## Folder structure (short)

ai-apis/
- .env — environment variables for the Python service
- api.py — FastAPI application and endpoints
- helpers/ — Python helper modules
	- gen_latex.py — LaTeX generation logic
	- job_analisys.py — functions to prepare graph/analytics data
	- salary_prediciton.py — salary prediction helpers (loads `pre_trained` models)
- data/ — CSV datasets used for training/analysis
- pre_trained/ — stored ML models and scalers (pickle files)
- notebooks/ — Jupyter notebooks for analysis and experimentation
- scripts/ — utility scripts (e.g. `salary_model.py`)

GenJobMVC/
- GenJobMVC.sln — Visual Studio solution
- GenJobMVC/Program.cs — app entry
- Controllers/ — MVC controllers (AccountController, ATSController, DashboardController, JobPostingController, etc.)
- Services/ — service classes (ATSScoringService, JobPostingService, ResumeParserService)
- Data/ — `AppDbContext.cs` (EF Core DB context)
- Models/ — C# model classes (JobPosting, CompanyPosting, AI_API, etc.)
- Views/ — Razor views for pages (Dashboard, ATS, Account, JobPosting)
- wwwroot/ — static assets (css, js, lib)

## High-level flow

The following diagram shows the complete architecture and data flow between all system components including the web UI, ASP.NET MVC backend, FastAPI, databases, and external APIs.

<img src="./Genjob flow.png" alt="Logo"/>


## ai-apis API endpoints and example return objects

Below are the primary endpoints exposed by `ai-apis/api.py` with example request/response shapes (inferred from code).

1) POST /json
- Purpose: Converts a payload containing a JSON string into an object.
- Request body: { "data": "<json-string>" }
- Response: Parsed JSON object (dynamic)

Example
Request body:
{
	"data": "{\"foo\": \"bar\"}"
}

Response:
{
	"foo": "bar"
}

2) POST /generate_resume
- Purpose: Generate LaTeX-based resume content and return a PDF URL.
- Request body shape: ResumeGenRequest
	- links: { name: url, ... }
	- candidate: { name, email, phone, linkedin, github, education: [{degree, university, year, cgpa?}], skills: [...], projects: [...], experience: [...], additional?: [...] }
	- job_description: { title, company, location?, requirements: [...] }
- Response: { "pdf_url": "https://latexonline.cc/compile?text=..." } or { "error": "Failed to generate LaTeX content" }

Example response object:
{
	"pdf_url": "https://latexonline.cc/compile?text=<url-encoded-latex>"
}

3) POST /string-process
- Purpose: Accepts raw LaTeX text and writes it to `resume.tex`, returning cleaned text.
- Request body: { "data": "<latex-text>" }
- Response: the cleaned LaTeX text (string)

4) GET /get-company-names
- Purpose: Return a list of company names used by the salary model/helpers.
- Response: { "company_list": ["Company A", "Company B", ...] }

5) POST /get-other-data
- Purpose: Return auxiliary data for a given company name.
- Request body: { "company_name": "ACME" }
- Response: { "data": <object> } — shape depends on `getOtherData` implementation in `helpers/salary_prediciton.py`.

6) POST /predict-salary
- Purpose: Predict salary given company, job role, location, and employment status.
- Request body: { "company_name": "ACME", "job_role": "Data Scientist", "location": "NYC", "status": "Full-time" }
- Response: { "predicted_salary": 85000 } (value returned from `getSalaryPrediction`)

7) GET /get-graph-data
- Purpose: Return job analysis graph data for UI visualizations.
- Response: object returned by `getGraphData()` — likely a serializable dict with nodes/edges or chart data.

8) POST /test
- Purpose: Echo the ResumeGenRequest payload for testing/validation.
- Request body: ResumeGenRequest
- Response: { "candidate": <candidate-object>, "job": <job_description>, "links": <links> }

## External API integrations

**Glassdoor API (via GenJobMVC):**
- Search jobs by role and location
- Search jobs by company name
- Retrieve job posting data including salary ranges, company info, and requirements

**oss-120b AI Model:**
- Used for intelligent resume generation and content optimization
- Integrated through the ai-apis service

**latexonline.cc API:**
- Converts LaTeX markup to downloadable PDF URLs
- No authentication required, public service


## How to run

### Prerequisites
**Install Redis Stack:**
```powershell
# Windows (using Chocolatey)
choco install redis-stack-server

# Or download from: https://redis.io/docs/install/install-stack/windows/
# Start Redis Stack: redis-stack-server
```

**Install MySQL:**
```powershell
# Windows (using Chocolatey)
choco install mysql

# Or download MySQL Community Server from: https://dev.mysql.com/downloads/mysql/
# Create database: CREATE DATABASE genjob_db;
```

### ai-apis (Python Backend)
```powershell
cd "d:\C-drive mini\GenJob\ai-apis"
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn api:app --reload --port 8000
```

### GenJobMVC (ASP.NET)
**Required NuGet Packages:**
```powershell
cd "d:\C-drive mini\GenJob\GenJobMVC\GenJobMVC"
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Redis.OM
dotnet add package iTextSharp
dotnet add package DocumentFormat.OpenXml
```

**Run the application:**
```powershell
# Option 1: Visual Studio
# Open GenJobMVC/GenJobMVC.sln in Visual Studio and run

# Option 2: Command line
cd "d:\C-drive mini\GenJob\GenJobMVC\GenJobMVC"
dotnet ef database update
dotnet run
```

**individual contributors:**
- Devan Chauhan (https://github.com/Devan019)
	- .net REST Apis
	- ML prediction model/data analysis
	- Redis caching
	- GenAI integration
	- LaTeX template design
	- UI/UX design
- Moksh Desai (https://github.com/MokshDesai)
	- External package Resume parsing (iTextSharp/OpenXML)
	- MVC controllers and views
	- SharpAPI for ATS Score calculation
- Nishant Dholakia (https://github.com/Nishant-Dholakia)
	- Database (MySQL) Design
	- Glassdoor API
	- Identity framework
	- EF Core integration
	
## Notes and assumptions

- The README API docs are derived from `ai-apis/api.py` and helper names; exact response structures from helpers (e.g., `getOtherData`, `getGraphData`) depend on their implementations under `ai-apis/helpers`.
- The project uses `latexonline.cc` for LaTeX compilation by building a URL with the encoded LaTeX content. For production usage consider a private LaTeX build service or a server-side container to compile PDFs more securely.

---

If you want, I can:
- Add a dedicated `ai-apis/README.md` with detailed examples and curl/HTTPie calls.
- Create Postman collection or OpenAPI (FastAPI already provides /docs and /openapi.json).
- Add a small example script that calls `/generate_resume` with a sample payload and saves the returned PDF URL.

