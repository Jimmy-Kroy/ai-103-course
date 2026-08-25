
import os
from dotenv import load_dotenv

# Load variables from .env into the environment
load_dotenv()

# Read the value
foundry_endpoint = os.getenv("FOUNDRY_PROJECT_ENDPOINT")

print(f"FOUNDRY_PROJECT_ENDPOINT: {foundry_endpoint}")