import asyncio
import os
import logging
from dotenv import load_dotenv
from pathlib import Path

from azure.identity.aio import DefaultAzureCredential
from agent_framework import ChatAgent
from agent_framework.azure import AzureAIAgentClient
import plugins.log_plugin as log_plugin
from plugins.log_plugin import log_functions
import plugins.devops_plugin as devops_plugin
from plugins.devops_plugin import devops_functions

# Import logging configuration
from log_util import LogUtil, vdebug

# Import diagram generator
from diagram_generator import MermaidDiagramGenerator

# Load environment variables early
load_dotenv()

# Read logging configuration from environment
verbose_output = os.getenv("VERBOSE_OUTPUT", "false") == "true"
create_mermaid_diagram = os.getenv("CREATE_MERMAID_DIAGRAM", "false") == "true"
log_directory = os.getenv("LOG_DIRECTORY", "data/logs")
outcome_directory = os.getenv("OUTCOME_DIRECTORY", "data/outcome")
ticket_folder = outcome_directory  # Write tickets to outcome directory

# Set outcome directory for plugins
log_plugin.OUTCOME_DIRECTORY = outcome_directory
devops_plugin.OUTCOME_DIRECTORY = outcome_directory

# Setup logging with explicit parameters
logging_config = LogUtil()
logging_config.setup_logging(verbose=verbose_output)

async def main():
    logging.info("Starting incident resolution process...")

    # Setup directories
    script_dir = Path(__file__).parent
    log_path = script_dir / log_directory
    outcome_path = script_dir / outcome_directory
    
    # Create directories if they don't exist
    log_path.mkdir(parents=True, exist_ok=True)
    outcome_path.mkdir(parents=True, exist_ok=True)
    
    logging.info(f"Log directory: {log_path}")
    logging.info(f"Outcome directory: {outcome_path}")

    # Get configuration settings
    project_endpoint = os.getenv("PROJECT_ENDPOINT")
    model_deployment = os.getenv("MODEL_DEPLOYMENT")
    
    if not project_endpoint or not model_deployment:
        logging.error(
            "Missing required environment variables PROJECT_ENDPOINT or MODEL_DEPLOYMENT. "
            "Set them in your .env file before running."
        )
        return

    logging.info(f"Using project endpoint: {project_endpoint}")
    logging.info(f"Using model deployment: {model_deployment}")

    # Create the orchestrator agent using Microsoft Agent Framework
    logging.info("Initializing Orchestrator Agent with Agent Framework...")
    async with (
        DefaultAzureCredential(
            exclude_environment_credential=True,
            exclude_managed_identity_credential=True) as credential,
        ChatAgent(
            chat_client=AzureAIAgentClient(
                project_endpoint=project_endpoint,
                model_deployment_name=model_deployment,
                async_credential=credential
            ),
            instructions="""You are an orchestrator that coordinates incident resolution.

CRITICAL: You have access to these functions that you MUST use:
- read_log_file(filepath) - reads the log file and shows current state including any actions taken
- restart_service(service_name, logfile) - restarts a failing service
- rollback_transaction(logfile) - rollbacks a failed transaction  
- redeploy_resource(resource_name, logfile) - redeploys a resource
- increase_quota(logfile) - increases resource quota
- escalate_issue(logfile) - escalates when unable to resolve

Your workflow on EVERY iteration:
1. ALWAYS call read_log_file(filepath) FIRST to see the current state
2. Look for ERROR or CRITICAL entries in the log
3. Check the "ACTIONS IN PROGRESS" section to see what was already attempted
4. If NO errors exist OR errors have been resolved by previous actions, respond with "No action needed"
5. If errors still exist, call ONE devops function to fix the issue (avoid repeating actions that failed)
6. If same action tried multiple times without success, call escalate_issue()

RULES:
- MUST call read_log_file() at the start of every turn
- Only take ONE action per iteration
- Check what actions were already attempted before taking new action
- Be concise in your responses
- Focus on resolving the issue efficiently""",
            name="incident_orchestrator",
            tools=list(log_functions) + list(devops_functions)
        ) as agent,
    ):
        logging.info("Orchestrator agent created successfully.")
        
        # Process each log file
        for log_file in log_path.glob("*.log"):
            filename = log_file.name
            logfile_path = str(log_file)
            
            logging.info(f"\n{'='*60}")
            logging.info(f"Processing log file: {filename}")
            logging.info(f"{'='*60}")
            print(f"\n{'='*60}")
            print(f"Processing log file: {filename}")
            print(f"{'='*60}\n")
            
            # Print log summary before analysis
            log_plugin.print_log_summary(logfile_path)
            
            max_iterations = 5
            iteration = 0
            resolved = False
            total_tokens_in = 0
            total_tokens_out = 0
            
            while iteration < max_iterations and not resolved:
                iteration += 1
                logging.info(f"Iteration {iteration}/{max_iterations}")
                
                # Create the prompt for this iteration
                if iteration == 1:
                    prompt = f"Read the log file '{logfile_path}' using read_log_file() and analyze it. Then take appropriate corrective action if needed."
                else:
                    prompt = f"Read the log file '{logfile_path}' using read_log_file() to check if the previous issue has been resolved. If resolved, respond with 'No action needed'. If not, take further action."
                
                logging.debug(f"Prompt: {prompt}")
                
                try:
                    # Run the agent
                    logging.info("Running orchestrator agent...")
                    result = await agent.run(prompt)
                    
                    response_content = result.text
                    logging.info(f"Agent response: {response_content}")
                    print(f"Iteration {iteration}: {response_content}\n")
                    
                    # Track token usage
                    if hasattr(result, 'usage') and result.usage:
                        token_usage_in = getattr(result.usage, 'input_tokens', 0)
                        token_usage_out = getattr(result.usage, 'output_tokens', 0)
                        total_tokens_in += token_usage_in
                        total_tokens_out += token_usage_out
                        logging.debug(f"Token usage - Input: {token_usage_in}, Output: {token_usage_out}")
                    
                    # Check if resolved
                    if "no action needed" in response_content.lower():
                        resolved = True
                        outcome_msg = f"Issue in {filename} resolved after {iteration} iteration(s)."
                        logging.info(outcome_msg)
                        print(f"\n{outcome_msg}\n")
                    elif "escalate" in response_content.lower():
                        outcome_msg = f"Issue in {filename} escalated after {iteration} iteration(s)."
                        logging.warning(outcome_msg)
                        print(f"\n{outcome_msg}\n")
                        break
                    
                except Exception as e:
                    error_msg = f"Error during iteration {iteration}: {e}"
                    logging.error(error_msg)
                    print(f"{error_msg}\n")
                    # Don't break, try next iteration unless it's the last one
                    if iteration >= max_iterations:
                        break
                
                # Small delay between iterations to avoid rate limits
                if iteration < max_iterations and not resolved:
                    await asyncio.sleep(2)
            
            # Write outcome file
            final_resolution = "Resolved" if resolved else "Escalated" if "escalate" in response_content.lower() else "Max iterations reached"
            outcome_text = f"""Incident Resolution Summary
Log File: {filename}
Status: {final_resolution}
Iterations: {iteration}
Total Token Usage: Input={total_tokens_in}, Output={total_tokens_out}, Total={total_tokens_in + total_tokens_out}

Final Response:
{response_content}
"""
            log_plugin.write_outcome(logfile_path, outcome_text)
            logging.info(f"Outcome written to {outcome_directory}/{filename.replace('.log', '-outcome.log')}")
            
            # Generate diagram if enabled
            if create_mermaid_diagram:
                logging.info("Generating Mermaid diagram...")
                # Compose original issue summary from the log (first ERROR/CRITICAL lines)
                try:
                    with open(logfile_path, 'r', encoding='utf-8') as lf:
                        log_lines = lf.readlines()
                    error_lines = [l.strip() for l in log_lines if (' ERROR ' in l) or (' CRITICAL ' in l) or ('Error' in l) or ('CRITICAL' in l)]
                    # Take up to first 5 error lines and join into a single summary
                    original_issue = ' | '.join(error_lines[:5]) if error_lines else '(no error lines found)'
                except Exception:
                    original_issue = '(unable to read original log)'

                # resolution_summary: include the outcome_text we just wrote
                resolution_summary = outcome_text

                diagram_generator = MermaidDiagramGenerator(ticket_folder_path=ticket_folder)
                diagram_generator.save_diagram_file(
                    log_filename=filename,
                    resolution=final_resolution,
                    iterations=iteration,
                    token_usage_in=total_tokens_in,
                    token_usage_out=total_tokens_out,
                    original_issue=original_issue,
                    resolution_summary=resolution_summary,
                    final_response=response_content,
                )

    logging.info("\n" + "="*60)
    logging.info("Incident resolution process completed.")
    logging.info("="*60)


# Start the app
if __name__ == "__main__":
    asyncio.run(main())