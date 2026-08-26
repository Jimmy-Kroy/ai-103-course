import os
from dotenv import load_dotenv
from openai import OpenAI
from azure.identity import DefaultAzureCredential, get_bearer_token_provider

# Load variables from .env into the environment
load_dotenv()

# Read the value
foundry_endpoint = os.getenv("FOUNDRY_PROJECT_ENDPOINT")
deployment_name = os.getenv("DEPLOYMENT_NAME")
scope = os.getenv("SCOPE")

print(f"FOUNDRY_PROJECT_ENDPOINT: {foundry_endpoint}")
print(f"DEPLOYMENT_NAME: {deployment_name}")
print(f"SCOPE: {scope}")

token_provider = get_bearer_token_provider(DefaultAzureCredential(), scope)

client = OpenAI(
    base_url=foundry_endpoint,
    api_key=token_provider
)

response = client.responses.create(
    model=deployment_name,
    input="What is the capital of the Netherlands ?",
)

print(f"answer: {response.output[0]}")

# answer: ResponseOutputMessage(id='msg_03fdeeeca4524ae6006a8ef44ae0788197b1b00c08b6fbbbfe', content=[ResponseOutputText(annotations=[], text='The capital of the Netherlands is **Amsterdam**.', type='output_text', logprobs=[])], role='assistant', status='completed', type='message', phase=None)