# PowerShell script to set up the Python virtual environment and install dependencies

Write-Host "Setting up Python virtual environment for Prompt Engineering demo..." -ForegroundColor Green

# Create virtual environment
python -m venv .venv

# Activate virtual environment
.\.venv\Scripts\Activate.ps1

# Upgrade pip
python -m pip install --upgrade pip

# Install dependencies
pip install -r requirements.txt

Write-Host "Setup complete! Virtual environment created and dependencies installed." -ForegroundColor Green
Write-Host "To activate the virtual environment, run: .\.venv\Scripts\Activate.ps1" -ForegroundColor Yellow
Write-Host "To run the notebook, execute: jupyter notebook prompt-engineering.ipynb" -ForegroundColor Yellow
