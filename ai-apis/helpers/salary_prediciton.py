import pandas as pd
import pickle
from pathlib import Path
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import MinMaxScaler
import json
current_folder = Path(__file__).parent.parent

model_path = current_folder / "pre_trained" / "salary_2022" / "model.pkl"
scalar_path = current_folder / "pre_trained" / "salary_2022" / "scaler.pkl"

dataset = current_folder / "data" / "Salary_Dataset_with_Extra_Features.csv"

#data of main set
data = pd.read_csv(dataset)

#extract data for model
model_data = pd.DataFrame({
  "company_name" : data["Company Name"],
  "job_roles" : data["Job Title"],
  "location" : data["Location"],
  "salary" : data["Salary"],
  "employment_status" : data["Employment Status"]
})
#only input data with one-hot encoding
input_data = pd.get_dummies(data=model_data, columns=["company_name", "job_roles", "location", "employment_status"], dtype=int).drop(columns=["salary"])
#model load
with open(model_path, "rb") as f:
  model: LinearRegression = pickle.load(f)

#scalar load
with open(scalar_path, "rb") as f:
  scalar: MinMaxScaler = pickle.load(f)

#prediction fun
def getSalaryPrediction(company_name, job_roles, location, employment_status):
  #make dataframe
  user_data = pd.DataFrame({
    "company_name" : [company_name],
    "job_roles" : [job_roles],
    "location" : [location],
    "employment_status" : [employment_status]
  })

  #encoding
  user_data_encoded = pd.get_dummies(user_data, columns=["company_name","job_roles", "location", "employment_status"], dtype=int)
  #reindex and fill - 0
  user_data_encoded = user_data_encoded.reindex(columns=input_data.columns, fill_value=0)
  #predict
  predict_val = model.predict(X=user_data_encoded)

  #get salary
  salary = scalar.inverse_transform(predict_val.reshape(-1, 1))[0][0]

  return f"₹{salary:,.2f}"

#get companies
def getCompanies():
    companies = model_data["company_name"].dropna().unique().tolist()
    return companies

#get other data
def getOtherData(company_name: str):
  mask = model_data["company_name"].str.lower() == company_name.lower()
  other_data = model_data[mask]

  job_roles =  other_data["job_roles"].unique().tolist()
  location = other_data["location"].unique().tolist()
  employment_status = other_data["employment_status"].unique().tolist()

  other_data = {
    "job_roles" : job_roles,
    "location" : location,
    "employment_status" :employment_status
  }

  return other_data




if __name__ == "__main__":
  company_name = "IBM"
  job_roles = "Web"
  location = "Bangalore"
  employment_status = "Full Time"
  print(getSalaryPrediction(company_name, job_roles, location, employment_status))
  
