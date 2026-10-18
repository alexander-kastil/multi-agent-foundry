#!/bin/bash

# Script to set up the Python virtual environment and install dependencies

echo "Setting up Python virtual environment for Prompt Engineering demo..."

# Create virtual environment
python3 -m venv .venv

# Activate virtual environment
source .venv/bin/activate

# Upgrade pip
pip install --upgrade pip

# Install dependencies
pip install -r requirements.txt

echo "Setup complete! Virtual environment created and dependencies installed."
echo "To activate the virtual environment, run: source .venv/bin/activate"
echo "To run the notebook, execute: jupyter notebook prompt-engineering.ipynb"
