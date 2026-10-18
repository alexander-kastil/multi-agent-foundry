from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from azure.ai.inference.prompts import PromptTemplate
from dotenv import load_dotenv
import os

load_dotenv()

def get_chat_response(messages, context):
    project_client = AIProjectClient(
        endpoint=os.environ["PROJECT_ENDPOINT"],
        credential=DefaultAzureCredential()
    )

    template = PromptTemplate.from_string(
        """
        system:
        You are an AI assistant that is a chef specialized on Italian cuisine. Be friendly and helpful.
        
        Call the user by its first name which is {{name}}.
        """
    )

    system_message = template.create_messages()

    # add the prompt messages to the user messages
    chat = project_client.get_openai_client()
    return chat.chat.completions.create(
        model=os.environ["MODEL"],
        messages=system_message + messages,
        temperature=1,
        frequency_penalty=0.5,
        presence_penalty=0.5,
    )

if __name__ == "__main__":
    response = get_chat_response(
        messages=[{"role": "user", "content": "Can you instruct me on how to cook red pesto?"}],
        context={"name": "Alex"},
    )    

    # Clear terminal screen
    os.system('cls' if os.name == 'nt' else 'clear')

    print(response.choices[0].message.content)