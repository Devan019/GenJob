from groq import Groq
import os
import dotenv
dotenv.load_dotenv()

client = Groq(api_key=os.getenv("GROQ_API_KEY"))


def generate_latex(education, skills, experience, projects, additional, company_details) -> str:
    prompt = f"""
You are a resume generator.

Input:
Candidate:
{{
  "education": {education},
  "skills": {skills},
  "projects": {projects},
  "experience": {experience},
  "additional": {additional}
}}
Job: {company_details}

Task:
Return a valid JSON with keys: "education", "skills", "experience", "projects", "additional".  
Each value = one LaTeX string.

Rules:
1. Escape backslashes as `\`.  
2. No preamble (`\documentclass`, `\begin{{document}}`, `\end{{document}}`).  
3. No `%`, comments, or `\n`.  
4. Format:  
   - Education: `\section*{{Education}} \begin{{itemize}} ... \end{{itemize}}`  
   - Skills: bullet points, one per category:  
     ```
     \section*{{Skills}}
     \begin{{itemize}}
       \item \textbf{{Languages}}: ...
       \item \textbf{{Frameworks}}: ...
     \end{{itemize}}
     ```
   - Experience/Projects: `\section*{{...}} \textbf{{Role, Org}} \hfill Date \begin{{itemize}} ... \end{{itemize}}`  
   - Additional: same style, or `""` if empty.  
5. Strict JSON: double quotes, no trailing commas.  
6. Use /% instead of only %
Output JSON only.
"""

    completion = client.chat.completions.create(
        model="openai/gpt-oss-120b",
        messages=[
            {
                "role": "user",
                "content": prompt
            }
        ],
        temperature=1,
        max_completion_tokens=32321,
        top_p=1,
        reasoning_effort="medium",
        stream=True,
        stop=None,
        tools=[{"type": "browser_search"}]
    )
    data = ""
    for chunk in completion:
        data += chunk.choices[0].delta.content or ""
    return data
