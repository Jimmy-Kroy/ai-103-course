
import os
from dotenv import load_dotenv
from openai import OpenAI

# Load variables from .env into the environment
load_dotenv()

# Read the value
foundry_endpoint = os.getenv("FOUNDRY_PROJECT_ENDPOINT")
api_key = os.getenv("API_KEY")
deployment_name = os.getenv("DEPLOYMENT_NAME")

print(f"FOUNDRY_PROJECT_ENDPOINT: {foundry_endpoint}")
print(f"API_KEY: {api_key}")
print(f"DEPLOYMENT_NAME: {deployment_name}")

client = OpenAI(
    base_url=foundry_endpoint,
    api_key=api_key
)

response = client.responses.create(
    model=deployment_name,
    input="What is the capital of France?",
)

print(f"answer: {response.output[0]}")

# answer: ResponseOutputMessage(id='msg_0c0b413d43d48fcd006a8edba6502c8193ac219688ca7fddb7', 
#                               content=[ResponseOutputText(annotations=[], text='Paris.', type='output_text', logprobs=[])], role='assistant', status='completed', type='message', phase=None)