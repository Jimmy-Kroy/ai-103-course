import functions
import os
import json
from dotenv import load_dotenv

# Add references
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
# from azure.ai.projects.models import PromptAgentDefinition, FunctionTool
from azure.ai.projects.models import PromptAgentDefinition, FunctionTool, Reasoning
from openai.types.responses.response_input_param import FunctionCallOutput, ResponseInputParam
from functions import next_visible_event, calculate_observation_cost, generate_observation_report


def print_reasoning(output_items):
    """Print readable reasoning summaries from a response's output items.

    Note: `encrypted_content` cannot be decrypted client-side (the key is held
    by the service), so we only surface the summary text and report the blob size.
    """
    for item in output_items:
        if item.type != "reasoning":
            continue

        summaries = [s.text for s in (item.summary or []) if getattr(s, "text", None)]
        if summaries:
            print("REASONING SUMMARY:")
            for text in summaries:
                print(f"  {text}")
        else:
            print("REASONING: no summary returned by the model.")

        if item.encrypted_content:
            print(f"  (encrypted_content present, {len(item.encrypted_content)} chars, not decryptable client-side)")


def _stub_next_visible_event(location: str) -> str:
    """Return a minimal next_visible_event JSON string for testing."""
    return json.dumps({"event": "Mock Event", "date": "01-01"})


def call_all_functions():
    # Ensure private loaders can be invoked directly
    try:
        events = functions._load_events()
        print(f"_load_events returned {len(events)} events")
    except Exception as e:
        print(f"_load_events failed: {e}")

    try:
        rates = functions._load_rates("../../data/telescope_rates.txt")
        print(f"_load_rates returned {len(rates)} rates")
    except Exception as e:
        print(f"_load_rates failed: {e}")

    # Call calculate_observation_cost (may return an error JSON string)
    try:
        # use a valid telescope tier (standard|advanced|premium) instead of 'basic'
        cost_json = functions.calculate_observation_cost("standard", 1.5, "low")
        print(f"calculate_observation_cost -> {cost_json}")
    except Exception as e:
        print(f"calculate_observation_cost failed: {e}")

    # Provide a stub for next_visible_event so generate_observation_report can run
    functions.next_visible_event = _stub_next_visible_event

    try:
        # match the tier used above so the report generation succeeds
        report_json = functions.generate_observation_report(
            "Mock Event", "Nowhere", "standard", 1.5, "low", "Test Observer"
        )
        print(f"generate_observation_report -> {report_json}")
    except Exception as e:
        print(f"generate_observation_report failed: {e}")

def main(): 
    # Clear the console
    os.system('cls' if os.name=='nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    project_endpoint = os.getenv("PROJECT_ENDPOINT")
    model_deployment = os.getenv("MODEL_DEPLOYMENT_NAME")

    print(f"PROJECT_ENDPOINT={project_endpoint}")
    print(f"MODEL_DEPLOYMENT_NAME={model_deployment}")

    # Connect to the project client
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=project_endpoint, credential=credential) as project_client,
        project_client.get_openai_client() as openai_client,
    ):

        # Define the event function tool
        event_tool = FunctionTool(
            name="next_visible_event",
            description="Get the next visible event in a given location.",
            parameters={
                "type": "object",
                "properties": {
                    "location": {
                        "type": "string",
                        "description": "continent to find the next visible event in (e.g. 'north_america', 'south_america', 'australia')",
                    },
                },
                "required": ["location"],
                "additionalProperties": False,
            },
            strict=True,
        )

        # Define the observation cost function tool
        cost_tool = FunctionTool(
            name="calculate_observation_cost",
            description="Calculate the cost of an observation based on the telescope tier, number of hours, and priority level.",
            parameters={
                "type": "object",
                "properties": {
                    "telescope_tier": {
                        "type": "string",
                        "description": "the tier of the telescope (e.g. 'standard', 'advanced', 'premium')",
                    },
                    "hours": {
                        "type": "number",
                        "description": "the number of hours for the observation",
                    },
                    "priority": {
                        "type": "string",
                        "description": "the priority level of the observation (e.g. 'low', 'normal', 'high')",
                    },
                },
                "required": ["telescope_tier", "hours", "priority"],
                "additionalProperties": False,
            },
            strict=True,
        )

        # Define the observation report generation function tool
        report_tool = FunctionTool(
            name="generate_observation_report",
            description="Generate a report summarizing an astronomical observation",
            parameters={
                "type": "object",
                "properties": {
                    "event_name": {
                        "type": "string",
                        "description": "the name of the astronomical event being observed",
                    },
                    "location": {
                        "type": "string",
                        "description": "the location of the observer",
                    },
                    "telescope_tier": {
                        "type": "string",
                        "description": "the tier of the telescope used for the observation (e.g. 'standard', 'advanced', 'premium')",
                    },
                    "hours": {
                        "type": "number",
                        "description": "the number of hours the telescope was used for the observation",
                    },
                    "priority": {
                        "type": "string",
                        "description": "the priority level of the observation (e.g. 'low', 'normal', 'high')",
                    },
                    "observer_name": {
                        "type": "string",
                        "description": "the name of the person who conducted the observation",
                    },                   
                },
                "required": ["event_name", "location", "telescope_tier", "hours", "priority", "observer_name"],
                "additionalProperties": False,
            },
            strict=True,
        )

        # Create a new agent with the function tools
        agent = project_client.agents.create_version(
            agent_name="astronomy-agent",
            definition=PromptAgentDefinition(
                model=model_deployment,
                instructions=
                    """You are an astronomy observations assistant that helps users find 
                    information about astronomical events and calculate telescope rental costs. 
                    Use the available tools to assist users with their inquiries.""",
                tools=[event_tool, cost_tool, report_tool],
                reasoning=Reasoning(summary="auto"),
            ),
        )

        # Create a thread for the chat session
        conversation = openai_client.conversations.create()

        while True:
            user_input = input("Enter a prompt for the astronomy agent. Use 'quit' to exit.\nUSER: ").strip()
            if user_input.lower() == "quit":
                print("Exiting chat.")
                break

            # Create a list to hold function call outputs that will be sent back as input to the agent
            input_list: ResponseInputParam = []

            # Send a prompt to the agent
            openai_client.conversations.items.create(
                conversation_id=conversation.id,
                items=[{"type": "message", "role": "user", "content": user_input}],
            )

            # Retrieve the agent's response, which may include function calls
            response = openai_client.responses.create(
                conversation=conversation.id,
                extra_body={"agent_reference": {"name": agent.name, "type": "agent_reference"}},
                input=input_list,
            )

            # Check the run status for failures
            if response.status == "failed":
                print(f"Response failed: {response.error}")
            else:
                print(f"Response output: {response.output}")
                print_reasoning(response.output)
                # Optionally still show the tool calls the model chose:
                for item in response.output:
                    if item.type == "function_call":
                        print(f"TOOL CALL: {item.name}({item.arguments})")


            # Process function calls
            for item in response.output:
                if item.type == "function_call":
                    # Retrieve the matching function tool
                    function_name = item.name
                    result = None
                    if item.name == "next_visible_event":
                        result = next_visible_event(**json.loads(item.arguments))
                    elif item.name == "calculate_observation_cost":
                        result = calculate_observation_cost(**json.loads(item.arguments))
                    elif item.name == "generate_observation_report":
                        result = generate_observation_report(**json.loads(item.arguments))

                    # Append the output text
                    input_list.append(
                        FunctionCallOutput(
                            type="function_call_output",
                            call_id=item.call_id,
                            output=result,
                        )
                    )
            
            # Send function call outputs back to the model and retrieve a response
            if input_list:
                print("INPUT LIST SENT TO AGENT:")
                print(json.dumps(input_list, indent=2, default=str))

                response = openai_client.responses.create(
                    conversation=conversation.id,
                    input=input_list,
                    extra_body={"agent_reference": {"name": agent.name, "type": "agent_reference"}},
                )
            # Display the agent's response
            print(f"AGENT: {response.output_text}")

        # Delete the agent when done
        project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
        print("Deleted agent.")


if __name__ == '__main__': 
    # call_all_functions()    # call_all_functions()
    # 'south_america' 'north_america' 'australia'
    # events = next_visible_event('australia')
    # print(events)
    # price = calculate_observation_cost(telescope_tier="premium", hours=2.5, priority="high")
    # print(price)
    # report = generate_observation_report(event_name="Solar Eclipse", location="south_america", telescope_tier="premium", hours=2.5, priority="high", observer_name="John Doe")
    # print(report) 
    main()
