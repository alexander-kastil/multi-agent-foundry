import os
import io
import sys
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


def main():
    """Delete agents in the Azure AI Foundry project."""
    # Clear the console
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")

    print(f"{'='*70}")
    print("🗑️  AGENT DELETION UTILITY")
    print(f"{'='*70}")
    print(f"Endpoint: {endpoint}")
    print()

    project_client = AIProjectClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )

    with project_client:
        # List all agents and their versions
        print("📋 Fetching all agents and versions...")
        agents = project_client.agents.list()

        agent_versions = []
        agent_counter = 0

        for agent in agents:
            try:
                versions = project_client.agents.list_versions(agent.name)
                for version in versions:
                    agent_counter += 1
                    # Extract model information
                    model_name = "Unknown"
                    if hasattr(version, 'model') and version.model:
                        model_name = version.model
                    elif hasattr(version, 'definition') and version.definition:
                        if hasattr(version.definition, 'model'):
                            model_name = version.definition.model

                    # Extract description
                    description = "No description"
                    if hasattr(version, 'description') and version.description:
                        description = version.description
                    elif hasattr(version, 'definition') and version.definition:
                        if hasattr(version.definition, 'instructions'):
                            desc = version.definition.instructions
                            if desc and len(desc) > 50:
                                description = desc[:47] + "..."
                            elif desc:
                                description = desc

                    agent_versions.append({
                        'number': agent_counter,
                        'agent_name': agent.name,
                        'version': version.version,
                        'model': model_name,
                        'description': description
                    })
            except Exception as e:
                print(f"⚠️  Could not fetch versions for agent '{agent.name}': {e}")

        if not agent_versions:
            print("\n✅ No agent versions found. Nothing to delete.")
            return

        print(f"\n📊 Found {len(agent_versions)} agent version(s):")
        print(f"{'─'*100}")
        print(f"{'#':<3} {'Agent Name':<20} {'Version':<8} {'Model':<20} {'Description'}")
        print(f"{'─'*100}")

        for av in agent_versions:
            model_display = av['model'][:18] + '...' if len(av['model']) > 18 else av['model']
            desc_display = av['description'][:45] + '...' if len(av['description']) > 45 else av['description']
            print(f"{av['number']:<3} {av['agent_name']:<20} {av['version']:<8} {model_display:<20} {desc_display}")

        print(f"{'─'*100}")
        print()
        print("Select agent versions to delete:")
        print("  - Enter numbers separated by commas (e.g., 1,3,8)")
        print("  - Enter 'all' to delete all agent versions")
        print("  - Enter 'q' to quit")
        print()

        selection = input("Your selection: ").strip().lower()

        if selection == 'q':
            print("\n✅ Cancelled. No agents were deleted.")
            return

        # Determine which agent versions to delete
        versions_to_delete = []

        if selection == 'all':
            versions_to_delete = agent_versions
        else:
            try:
                indices = [int(x.strip()) for x in selection.split(',')]
                for idx in indices:
                    matching_versions = [av for av in agent_versions if av['number'] == idx]
                    if matching_versions:
                        versions_to_delete.extend(matching_versions)
                    else:
                        print(f"⚠️  Invalid selection: {idx} (out of range)")
            except ValueError:
                print("❌ Invalid input format. Please enter numbers separated by commas.")
                return

        if not versions_to_delete:
            print("\n✅ No agent versions selected. Nothing to delete.")
            return

        print(f"\n🗑️  Selected {len(versions_to_delete)} agent version(s) for deletion:")
        for av in versions_to_delete:
            print(f"  - {av['agent_name']}:{av['version']} ({av['model']})")

        print()
        final_confirm = input(f"⚠️  Proceed to delete {len(versions_to_delete)} agent version(s)? (yes/no): ").strip().lower()

        if final_confirm not in ['yes', 'y']:
            print("\n✅ Cancelled. No agents were deleted.")
            return

        print()
        print("🗑️  Deleting agent versions...")
        print(f"{'─'*70}")

        deleted_count = 0
        failed_count = 0

        for i, av in enumerate(versions_to_delete, 1):
            try:
                project_client.agents.delete_version(
                    agent_name=av['agent_name'],
                    agent_version=av['version']
                )
                print(f"✓ [{i}/{len(versions_to_delete)}] Deleted: {av['agent_name']}:{av['version']}")
                deleted_count += 1
            except Exception as e:
                print(f"✗ [{i}/{len(versions_to_delete)}] Failed to delete {av['agent_name']}:{av['version']}: {e}")
                failed_count += 1

        print(f"{'─'*70}")
        print()
        print(f"{'='*70}")
        print("📊 SUMMARY")
        print(f"{'='*70}")
        print(f"✅ Successfully deleted: {deleted_count} agent version(s)")
        if failed_count > 0:
            print(f"❌ Failed to delete: {failed_count} agent version(s)")
        print(f"{'='*70}")


if __name__ == '__main__':
    main()
