
from typing import Dict, List, Optional, Union
from fastapi import FastAPI
from pydantic import BaseModel
import json
app = FastAPI()
import urllib.parse
from helpers.salary_prediciton import getOtherData, getSalaryPrediction, getCompanies
from helpers.job_analisys import getGraphData
from helpers.gen_latex import generate_latex
class EducationItem(BaseModel):
    degree: str
    university: str
    year: str
    cgpa: Optional[Union[str, float]] = None


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

class ResumeType(BaseModel):
    type: int


class ResumeGenRequest(BaseModel):
    links: Dict[str, str]
    candidate: Candidate
    job_description: JobDescription


class dataItem(BaseModel):
    data: str


@app.post("/json")
def jsonConverter(itme: dataItem):

    return json.loads(itme.data)



def clean_latex_content(text: str) -> str:
    """Clean LaTeX content to remove problematic Unicode characters"""
    # Replace common Unicode characters with ASCII equivalents
    replacements = {
        '\u2011': '-',  # non-breaking hyphen to regular hyphen
        '\u2013': '-',  # en dash to hyphen
        '\u2014': '-',  # em dash to hyphen
        '\u2018': "'",  # left single quote
        '\u2019': "'",  # right single quote
        '\u201c': '"',  # left double quote
        '\u201d': '"',  # right double quote
        '\u2022': '*',  # bullet point
    }
    
    for unicode_char, ascii_char in replacements.items():
        text = text.replace(unicode_char, ascii_char)
    
    return text

def convert_latex_to_pdf(latex_content: str) -> str:
    """
    Convert LaTeX content to PDF using latexonline.cc
    Returns the URL to the compiled PDF
    """
    try:
        # URL encode the LaTeX content
        encoded_latex = urllib.parse.quote(latex_content)
        
        # Construct the latexonline.cc URL
        latexonline_url = f"https://latexonline.cc/compile?text={encoded_latex}"
        
        return latexonline_url
        
    except Exception as e:
        print(f"Error converting LaTeX to PDF: {e}")
        return None

@app.post("/generate_resume")
def genWholeLatex(
    request: ResumeGenRequest
):
    resume_type = 2
    print("Generating LaTeX code...", request)
    latex_code = generate_latex(
        education=request.candidate.education,
        skills=request.candidate.skills,
        experience=request.candidate.experience,
        projects=request.candidate.projects,
        additional=request.candidate.additional,
        company_details=request.job_description.dict(),
        resume_type=resume_type
    )
    # print("Received LaTeX code from generator", latex_code)
    
    try:
        json_code = json.loads(latex_code)
    except json.JSONDecodeError as e:
        print(f"JSON parsing error: {e}")
        print(f"Raw response: {latex_code}")
        return {"error": "Failed to generate LaTeX content"}

    # Clean each section to remove Unicode characters
    for key in json_code:
        if isinstance(json_code[key], str):
            json_code[key] = clean_latex_content(json_code[key])

    # Build LaTeX links dynamically
    links_dict = request.links or {}
    links_latex = " \\,|\\, ".join([rf"\href{{{url}}}{{{name}}}" for name, url in links_dict.items()])

    # Horizontal layout (type=1) - Two columns
    if resume_type == 1:
        header_block = rf"""
\begin{{center}}
    {{\LARGE \textbf{{ {request.candidate.name} }}}} \\[2pt]
    \href{{mailto:{request.candidate.email}}}{{{request.candidate.email}}} \\,|\\, {request.candidate.phone} \\,|\\, {links_latex}
\end{{center}}
\vspace{{4pt}}
"""
        # Two-column layout for horizontal resume
        template = rf"""\documentclass[12pt]{{article}}
\usepackage[margin=0.7in]{{geometry}}
\usepackage{{helvet}}
\renewcommand{{\familydefault}}{{\sfdefault}}
\usepackage{{enumitem}}
\setlist[itemize]{{leftmargin=*,noitemsep,topsep=0pt}}
\usepackage[hidelinks]{{hyperref}}
\usepackage{{paracol}}
\pagenumbering{{gobble}}
\linespread{{1.1}}

\begin{{document}}
{header_block}
\begin{{paracol}}{{2}}
{json_code.get("education", "")}
{json_code.get("skills", "")}
\switchcolumn
{json_code.get("experience", "")}
{json_code.get("projects", "")}
\end{{paracol}}
{json_code.get("additional", "")}
\end{{document}}
"""
    
    # Vertical layout (type=2) - Single column
    else:
        header_block = rf"""
\begin{{center}}
    {{\LARGE \textbf{{ {request.candidate.name} }}}} \\[4pt]
    \href{{mailto:{request.candidate.email}}}{{{request.candidate.email}}} \\[2pt]
    {request.candidate.phone} \\[2pt]
    {links_latex}
\end{{center}}
\vspace{{4pt}}
"""
        # Single column layout for vertical resume
        template = rf"""\documentclass[12pt]{{article}}
\usepackage[margin=0.7in]{{geometry}}
\usepackage{{helvet}}
\renewcommand{{\familydefault}}{{\sfdefault}}
\usepackage{{enumitem}}
\setlist[itemize]{{leftmargin=*,noitemsep,topsep=0pt}}
\usepackage[hidelinks]{{hyperref}}
\pagenumbering{{gobble}}
\linespread{{1.1}}

\begin{{document}}
{header_block}
{json_code.get("education", "")}
{json_code.get("skills", "")}
{json_code.get("experience", "")}
{json_code.get("projects", "")}
{json_code.get("additional", "")}
\end{{document}}
"""

    # Clean the final template
    template = clean_latex_content(template)
    
    print("Generated LaTeX code")


    pdf_url = convert_latex_to_pdf(template)
    
    return {
        "pdf_url": pdf_url,
    }
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

@app.get("/get-company-names")
def getNames():
    data = getCompanies()
    return {"company_list" : data}

class OtherItemModel(BaseModel):
    company_name : str

@app.post("/get-other-data")
def otherData(item: OtherItemModel):
    data = getOtherData(item.company_name)
    return {"data" : data}

class PredictSalaryModel(BaseModel):
    company_name : str
    job_role:str
    location:str
    status:str

@app.post("/predict-salary")
def predictSalary(data: PredictSalaryModel):
    salary = getSalaryPrediction(company_name=data.company_name,
                                 job_roles=data.job_role,
                                 location=data.location,
                                 employment_status=data.status
                                )
    return {"predicted_salary" : salary}

@app.get("/get-graph-data")
def graphData():
    obj = getGraphData()
    return obj

@app.post("/test")
def TestGen(
    request: ResumeGenRequest
):
    return {
        "candidate" : request.candidate,
        "job" : request.job_description,
        "links" : request.links
    }
    

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("api:app", host="localhost", port=8000, reload=True)
