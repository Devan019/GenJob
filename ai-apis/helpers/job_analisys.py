import pandas as pd
from pathlib import Path
import json
current_folder = Path(__file__).parent.parent
path = current_folder / "data" / "Salary_Dataset_with_Extra_Features.csv"


data = pd.read_csv(path)

def getGraphData():
  roles_cnt  = data.groupby(by="Job Roles")["Employment Status"].count().sort_values(ascending=False).to_json() # role cnt data

  avg_salary = data.groupby(by="Job Roles")["Salary"].mean().round(0).to_json() # role - avg-salary

  company_cnt = data.groupby(by="Location")["Company Name"].count().to_json() # location-company

  location_salary = data.groupby(by="Location")["Salary"].mean().round().to_json() #location-salary

  rating = data.groupby(by="Location")["Rating"].mean().to_json() # location-rating

  obj = {
    "RolesCount" : json.loads(roles_cnt),
    "AverageSalary" : json.loads(avg_salary),
    "CompanyCount" : json.loads(company_cnt),
    "LocationSalary" : json.loads(location_salary),
    "Rating" : json.loads(rating)
  }

  # print(obj)

  return obj


if __name__ == "__main__":
  getGraphData()


