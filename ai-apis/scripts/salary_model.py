import pandas as pd
import numpy as np
from sklearn.preprocessing import MinMaxScaler
from sklearn.model_selection import train_test_split
from sklearn.linear_model import LinearRegression
import pickle

def main():
    # Load the dataset
    print("Loading dataset...")
    data = pd.read_csv("../data/Salary_Dataset_with_Extra_Features.csv")
    print(f"Dataset loaded with {len(data)} rows")
    
    # Create new DataFrame with selected columns
    print("Preprocessing data...")
    data = pd.DataFrame({
        "company_name": data["Company Name"],
        "job_roles": data["Job Roles"],
        "location": data["Location"],
        "salary": data["Salary"],
        "employment_status": data["Employment Status"]
    })
    
    print("Original data shape:", data.shape)
    print("\nData preview:")
    print(data.head())
    
    # One-hot encoding for categorical variables
    labels = ["company_name", "job_roles", "location", "employment_status"]
    data_encoded = pd.get_dummies(data=data, columns=labels)
    
    print(f"\nAfter encoding - Shape: {data_encoded.shape}")
    print("Encoded data preview:")
    print(data_encoded.head(1))
    
    # Scale the salary column
    print("\nScaling salary data...")
    scaler = MinMaxScaler()
    data_encoded["salary"] = scaler.fit_transform(data_encoded[["salary"]])
    
    print("Scaled data preview:")
    print(data_encoded.head(1))
    
    # Prepare features and target
    X = data_encoded.drop(columns=["salary"])
    y = data_encoded["salary"]
    
    print(f"\nFeatures shape: {X.shape}")
    print(f"Target shape: {y.shape}")
    
    # Split the data
    print("\nSplitting data into train/test sets...")
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.33, random_state=42)
    
    print(f"Training set: {X_train.shape}")
    print(f"Test set: {X_test.shape}")
    
    # Train the model
    print("\nTraining Linear Regression model...")
    model = LinearRegression()
    model.fit(X_train, y_train)
    
    # Evaluate the model
    train_score = model.score(X_train, y_train)
    test_score = model.score(X_test, y_test)
    
    print(f"\nModel Performance:")
    print(f"Training R² score: {train_score:.4f}")
    print(f"Test R² score: {test_score:.4f}")
    

    
    # Save the model and scaler
    print("\nSaving model and scaler...")
    with open("../pre_trained/salary_2022/model.pkl", "wb") as f:
        pickle.dump(model, f)

    with open("../pre_trained/salary_2022/scaler.pkl", "wb") as f:
        pickle.dump(scaler, f)
    
    print("Model saved as 'model.pkl'")
    print("Scaler saved as 'scaler.pkl'")
    
    # Create a prediction function
    def predict_salary(company_name, job_roles, location, employment_status):
        """Predict salary for new input"""
        # Create input DataFrame
        new_data = pd.DataFrame({
            "company_name": [company_name],
            "job_roles": [job_roles],
            "location": [location],
            "employment_status": [employment_status]
        })
        
        # Apply one-hot encoding
        new_data_encoded = pd.get_dummies(new_data, columns=labels)
        
        # Align columns with training data
        new_data_encoded = new_data_encoded.reindex(columns=X.columns, fill_value=0)
        
        # Make prediction
        scaled_prediction = model.predict(new_data_encoded)
        
        # Inverse transform to get actual salary
        salary = scaler.inverse_transform(scaled_prediction.reshape(-1, 1))[0][0]
        
        return salary
    
    # Test the prediction function
    print("\nTesting prediction function...")
    test_salary = predict_salary("IBM", "Web", "Bangalore", "Full Time")
    print(f"Predicted salary for IBM Web Developer in Bangalore (Full Time): ₹{test_salary:.2f}")
    
    print("\nScript completed successfully!")

if __name__ == "__main__":
    main()