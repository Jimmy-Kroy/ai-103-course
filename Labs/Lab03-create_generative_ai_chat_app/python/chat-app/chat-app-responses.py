import os
from dotenv import load_dotenv

# import namespaces
from openai import OpenAI
from azure.identity import DefaultAzureCredential, get_bearer_token_provider

def main():
    try:
        # Clear the console
        os.system('cls' if os.name == 'nt' else 'clear')
        print("App started")
        # Load environment variables from .env file (if present)
        load_dotenv()
        azure_openai_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
        model_deployment = os.getenv("MODEL_DEPLOYMENT")
        scope = os.getenv("SCOPE")
        print("azure_openai_endpoint =", azure_openai_endpoint)
        print("model_deployment =", model_deployment)
        print("scope =", scope)

        # Initialize the OpenAI client
        token_provider = get_bearer_token_provider(
            DefaultAzureCredential(), scope
        )

        openai_client = OpenAI(
            base_url=azure_openai_endpoint,
            api_key=token_provider
        )

        # Loop until the user wants to quit
        while True:
            input_text = input('\nEnter a prompt (or type "quit" to exit): ')
            if input_text.lower() == "quit":
                break
            if len(input_text) == 0:
                print("Please enter a prompt.")
                continue

            # Get a response
            response = openai_client.responses.create(
                         model=model_deployment,
                         instructions="You are a helpful AI assistant that answers questions and provides information.",
                         input=input_text
            )
            print(response.output_text)

    except Exception as ex:
        print(ex)


if __name__ == '__main__':
    main()