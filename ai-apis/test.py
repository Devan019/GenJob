import os
import subprocess
import cloudinary
import cloudinary.uploader
from dotenv import load_dotenv
load_dotenv()
# 🔹 Configure Cloudinary
cloudinary.config(
    cloud_name=os.getenv("CLOUDINARY_CLOUD_NAME"),
    api_key=os.getenv("CLOUDINARY_API_KEY"),
    api_secret=os.getenv("CLOUDINARY_API_SECRET")
)
from pylatex import Document
from pylatex.utils import NoEscape

def latex_to_pdf_from_file(tex_file, filename="output"):
    """
    Generates a PDF from a .tex file using PyLaTeX.
    Returns the PDF file path.
    """
    # Read LaTeX content from the file
    with open(tex_file, "r", encoding="utf-8") as f:
        latex_code = f.read()

    # Create a PyLaTeX Document
    doc = Document()
    doc.append(NoEscape(latex_code))  # insert raw LaTeX code

    # Generate PDF
    pdf_path = doc.generate_pdf(filename, clean_tex=False)
    return pdf_path



def upload_to_cloudinary(pdf_path):
    response = cloudinary.uploader.upload(
        pdf_path,
        resource_type="raw"  # PDFs are not images
    )
    return response["secure_url"]


if __name__ == "__main__":
    # Step 1: Load LaTeX file (example: sample.tex)
    tex_file = "resume.tex"
    if not os.path.exists(tex_file):
        print("❌ LaTeX file not found!")
        raise FileNotFoundError(f"{tex_file} not found!")

    # Step 2: Generate PDF
    pdf_file = latex_to_pdf_from_file(tex_file, filename="output")

    # Step 3: Upload to Cloudinary
    url = upload_to_cloudinary(pdf_file)

    print("✅ PDF uploaded:", url)
