from groq import Groq
import os
import dotenv

dotenv.load_dotenv()

client = Groq(api_key=os.getenv("GROQ_API_KEY"))


def generate_latex(education, skills, experience, projects, additional, company_details, resume_type) -> str:
    """
    resume_type:
        1 = horizontal layout (two columns, contact info in one line)
        2 = vertical layout (single column, contact info stacked)
    """
    layout_instruction = "horizontal two-column layout with contact info in one line at top" if resume_type == 1 else "vertical single-column layout with contact info stacked vertically"

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

IMPORTANT RULES:
1. Use ONLY ASCII characters - no Unicode characters, no special quotes, no em dashes, no non-breaking hyphens
2. Escape backslashes as `\`.  
3. No preamble (`\documentclass`, `\begin{{document}}`, `\end{{document}}`).  
4. No `%` comments. Use regular hyphens `-` instead of any special dashes.
5. Format:  
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
6. Strict JSON: double quotes, no trailing commas.  
7. Generate content **optimized for a {layout_instruction}**.
   
For horizontal layout (two columns):
- Keep sections concise and compact
- Use shorter bullet points
- Consider splitting content across two columns

For vertical layout (single column):
- Can use more detailed descriptions
- More space for each section

CRITICAL: Replace any special Unicode characters with their ASCII equivalents.
Use regular quotes ", regular hyphens -, and basic punctuation only.

Output JSON only.
"""

    completion = client.chat.completions.create(
    model="openai/gpt-oss-120b",
    messages=[{"role": "user", "content": prompt}],
    temperature=0.7,
    max_completion_tokens=2000,
    top_p=0.9,
    reasoning_effort="low",
    stream=False,
)

    data = completion.choices[0].message.content
    return data

