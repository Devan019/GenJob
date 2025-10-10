# GenJob — AI-assisted Job & Resume Toolkit

This repository contains two main components: an `ai-apis` microservice that provides AI-powered resume generation, salary prediction, and job analysis utilities; and `GenJobMVC`, an ASP.NET MVC web application that integrates those AI services into a web UI for employers and job seekers.

## Contents
- `ai-apis/` — Python FastAPI microservice providing AI helpers (resume LaTeX generation, salary prediction, job analysis, PDF conversion helper).
- `GenJobMVC/` — ASP.NET MVC web app (C#) that uses the microservice and provides UI pages (Dashboard, ATS, Resume generation, Salary prediction).

## Project features

- Resume generation (LaTeX) from structured candidate data and job description.
- Convert generated LaTeX to a PDF URL (uses latexonline.cc by default).
- Salary prediction for job roles by company, location and employment status.
- Job/role analytics and graph data for visualizations in the MVC app.
- Simple APIs to convert JSON strings and process LaTeX text.

## Tech stack

- ai-apis:
	- Python 3.10+
	- FastAPI for API endpoints
	- Pydantic for request validation
	- Helper scripts for ML/model inference (scikit-learn used in pre-trained model files)
	- latexonline.cc for LaTeX -> PDF compilation (URL-based)
- GenJobMVC:
	- .NET 8.0 (ASP.NET Core MVC)
	- C# for controllers, services and models
	- Entity Framework Core for data access/migrations
	- Razor views for UI

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

## High-level flow (Mermaid)

The following Mermaid diagram shows a simplified request flow between the web UI, the MVC backend, and the `ai-apis` microservice. It can be rendered by any tool that supports Mermaid (GitHub, VS Code Mermaid preview, Mermaid Live Editor).

```mermaid
flowchart LR
	UI[User (browser)] -->|Forms / Clicks| MVC[GenJobMVC (ASP.NET MVC)]
	MVC -->|calls REST| AIAPI[ai-apis (FastAPI)]
	AIAPI -->|generate_resume| LaTeXGen[gen_latex.py]
	LaTeXGen -->|returns JSON sections| AIAPI
	AIAPI -->|convert to PDF URL| latexonline[latexonline.cc]
	AIAPI -->|predict-salary| SalaryModel[pre_trained/salary_2022 model]
	AIAPI -->|graph data| JobAnalysis[job_analisys.py]
	MVC -->|renders view with results| UI
```

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


## How to run (short)

ai-apis (Python)

- Create a virtual environment and install dependencies from `ai-apis/requirements.txt`.
- Run the service with uvicorn:

```powershell
cd "d:\C-drive mini\GenJob\ai-apis"
python -m venv .venv; .\.venv\Scripts\Activate.ps1; pip install -r requirements.txt; uvicorn api:app --reload --port 8000
```

GenJobMVC (ASP.NET)

- Open `GenJobMVC/GenJobMVC.sln` in Visual Studio (or use `dotnet run` from the `GenJobMVC` folder).

## Notes and assumptions

- The README API docs are derived from `ai-apis/api.py` and helper names; exact response structures from helpers (e.g., `getOtherData`, `getGraphData`) depend on their implementations under `ai-apis/helpers`.
- The project uses `latexonline.cc` for LaTeX compilation by building a URL with the encoded LaTeX content. For production usage consider a private LaTeX build service or a server-side container to compile PDFs more securely.

---

If you want, I can:
- Add a dedicated `ai-apis/README.md` with detailed examples and curl/HTTPie calls.
- Create Postman collection or OpenAPI (FastAPI already provides /docs and /openapi.json).
- Add a small example script that calls `/generate_resume` with a sample payload and saves the returned PDF URL.

