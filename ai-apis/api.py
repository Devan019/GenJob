
from typing import List, Optional
from fastapi import FastAPI
from pydantic import BaseModel
import json
app = FastAPI()


class EducationItem(BaseModel):
    degree: str
    university: str
    year: str
    cgpa: Optional[str] = None


class ProjectItem(BaseModel):
    title: str
    description: str
    link: Optional[str] = None


class ExperienceItem(BaseModel):
    role: str
    company: str
    duration: str
    work: List[str]


class Candidate(BaseModel):
    name: str
    email: str
    phone: str
    linkedin: str
    github: str

    education: List[EducationItem]
    skills: List[str]
    projects: List[ProjectItem]
    experience: List[ExperienceItem]
    additional: Optional[List[str]] = []


class JobDescription(BaseModel):
    title: str
    company: str
    location: Optional[str] = None
    requirements: List[str]


@app.post("/generate_latex_snippet/")
def genLatexCode(candidate: Candidate, job_description: JobDescription):
    from helpers.gen_latex import generate_latex
    latex_code = generate_latex(
        education=candidate.education,
        skills=candidate.skills,
        experience=candidate.experience,
        projects=candidate.projects,
        additional=candidate.additional,
        company_details=job_description.dict()
    )
    return {"latex_code": latex_code}


class dataItem(BaseModel):
    data: str


@app.post("/json")
def jsonConverter(itme: dataItem):

    return json.loads(itme.data)


@app.post("/generate_latex/")
def genWholeLatex(candidate: Candidate, job_description: JobDescription):
    from helpers.gen_latex import generate_latex
    latex_code = generate_latex(
        education=candidate.education,
        skills=candidate.skills,
        experience=candidate.experience,
        projects=candidate.projects,
        additional=candidate.additional,
        company_details=job_description.dict()
    )
    code: str = latex_code
    json_code = json.loads(code)
    template = r"""\documentclass[10pt]{article}
\usepackage[margin=0.7in]{geometry}
\usepackage{helvet}
\renewcommand{\familydefault}{\sfdefault}
\usepackage{enumitem}
\setlist[itemize]{leftmargin=*,noitemsep,topsep=0pt}
\usepackage[hidelinks]{hyperref}
\pagenumbering{gobble}
\linespread{1.1}

\begin{document}

\begin{center}
    {\LARGE \textbf{ """ + candidate.name + r""" }} \\[2pt]
    \href{mailto:""" + candidate.email + r"""}{""" + candidate.email + r"""} \,|\, """ + candidate.phone + r""" \,|\, 
    \href{""" + candidate.linkedin + r"""}{""" + candidate.linkedin + r"""} \,|\, 
    \href{""" + candidate.github + r"""}{""" + candidate.github + r"""}
\end{center}
\vspace{4pt}
"""


    template += f"""{json_code["education"]}""" + f"""{json_code["skills"]}""" + f"""{json_code["experience"]}""" + f"""{json_code["projects"]}""" + f"""{json_code["additional"]}"""  + r"""\end{document}"""

    with open("resume.tex", "w") as f:
        f.write(template)

    return {"latex_code": template}

@app.post("/string-process")
def process_string(item: dataItem):
    latex_text = item.data
    
    # Fix URLs and other formatting
    latex_text = latex_text.replace('\\\\', '\\ ')
    latex_text = latex_text.replace('\\', '\ ')
    latex_text = latex_text.replace('%', '')

    with open("resume.tex", "w") as f:
        f.write(latex_text)


    return latex_text

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("api:app", host="localhost", port=8000, reload=True)
