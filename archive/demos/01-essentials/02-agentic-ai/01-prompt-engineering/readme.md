# Prompt Engineering for Agentic AI

This folder contains comprehensive Jupyter notebooks demonstrating prompt engineering techniques and advanced agentic AI patterns using Azure AI Foundry.

## Contents

### Notebooks

- `prompt-engineering.ipynb` - Interactive Jupyter notebook with prompt engineering examples and exercises
- `agentic-rag.ipynb` - **NEW**: Agentic Retrieval-Augmented Generation patterns and techniques
- `deep-reasoning.ipynb` - **NEW**: Deep reasoning capabilities including Chain-of-Thought, Tree of Thoughts, and Self-Consistency
- `react-framework.ipynb` - **NEW**: ReAct (Reasoning and Acting) framework for building autonomous agents

### Configuration and Setup

- `requirements.txt` - Python dependencies
- `.env.copy` - Environment configuration template
- `setup.sh` - Setup script for Linux/macOS
- `setup.ps1` - Setup script for Windows PowerShell
- `.gitignore` - Git ignore rules for virtual environment and generated files

## Getting Started

### Prerequisites

- Python 3.8 or higher
- Azure AI Foundry project with a deployed model
- Azure credentials configured for authentication

### Setup Instructions

#### Option 1: Using the Setup Script (Linux/macOS)

```bash
# Make the script executable (if not already)
chmod +x setup.sh

# Run the setup script
./setup.sh

# Activate the virtual environment
source .venv/bin/activate

# Copy the environment template and configure your credentials
cp .env.copy .env
# Edit .env with your Azure AI Foundry project details

# Launch Jupyter
jupyter notebook prompt-engineering.ipynb
```

#### Option 2: Using the Setup Script (Windows PowerShell)

```powershell
# Run the setup script
.\setup.ps1

# Activate the virtual environment
.\.venv\Scripts\Activate.ps1

# Copy the environment template and configure your credentials
Copy-Item .env.copy .env
# Edit .env with your Azure AI Foundry project details

# Launch Jupyter
jupyter notebook prompt-engineering.ipynb
```

#### Option 3: Manual Setup

```bash
# Create virtual environment
python3 -m venv .venv

# Activate virtual environment
source .venv/bin/activate  # On Windows: .venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Configure environment
cp .env.copy .env
# Edit .env with your credentials

# Launch Jupyter
jupyter notebook prompt-engineering.ipynb
```

### Configuration

Edit the `.env` file with your Azure AI Foundry project details:

```env
PROJECT_ENDPOINT="https://your-project.openai.azure.com/"
OPENAI_API_VERSION="2024-02-01"
MODEL="gpt-4.1-mini"
```

You can find these values in your Azure AI Foundry project settings.

## Notebook Topics

### Prompt Engineering (`prompt-engineering.ipynb`)

Covers fundamental prompt engineering techniques:

1. **Introduction to Prompt Engineering** - What is prompt engineering and why it matters
2. **Basic Prompt Structures** - Vague vs. specific prompts, role-based prompting, output format control
3. **Prompt Refinement Techniques** - Few-shot learning, using delimiters and structure, temperature control
4. **Chain of Thought Prompting** - Basic CoT, few-shot CoT, self-consistency techniques
5. **Advanced Patterns** - Instruction following with constraints, iterative refinement, meta-prompting
6. **Best Practices Summary** - Key principles and practical exercises

### Agentic RAG (`agentic-rag.ipynb`)

Demonstrates building intelligent RAG systems with agent capabilities:

1. **Introduction to Agentic RAG** - Understanding agentic vs. traditional RAG
2. **Traditional RAG vs. Agentic RAG** - Side-by-side comparison
3. **Building an Agentic RAG System** - Query decomposition, query reformulation, iterative retrieval
4. **Advanced Agentic RAG Patterns** - Corrective RAG (self-correction), adaptive retrieval strategies
5. **Production Best Practices** - Implementation guidance and optimization techniques

**Key Features:**
- Dynamic query planning and reformulation
- Self-reflection and iterative refinement
- Multi-step reasoning for complex questions
- Transparent reasoning traces

### Deep Reasoning (`deep-reasoning.ipynb`)

Explores advanced reasoning techniques for complex problem-solving:

1. **Chain-of-Thought Reasoning** - Basic CoT, few-shot CoT, complex problem solving
2. **Tree of Thoughts** - Generating and evaluating multiple reasoning paths
3. **Self-Consistency** - Multiple reasoning paths with majority voting
4. **Multi-Step Problem Solving** - Least-to-Most prompting for complex problems
5. **Reasoning with Verification** - Self-verification and multi-agent verification
6. **Production Considerations** - Cost, latency, and reliability trade-offs

**Key Techniques:**
- Chain-of-Thought (CoT)
- Tree of Thoughts (ToT)
- Self-Consistency
- Least-to-Most Prompting
- Self-Verification

### ReAct Framework (`react-framework.ipynb`)

Implements the Reasoning and Acting framework for autonomous agents:

1. **Introduction to ReAct** - Core concepts: Thought → Action → Observation loop
2. **Basic ReAct Pattern** - Simple agent implementation
3. **ReAct with Tool Usage** - Enhanced tool handling and execution
4. **Multi-Step ReAct Reasoning** - Complex multi-step problem solving
5. **Error Handling and Recovery** - Robust error recovery mechanisms
6. **Advanced ReAct Patterns** - ReAct with self-reflection and meta-reasoning

**Key Capabilities:**
- Interleaved reasoning and acting
- Tool usage and orchestration
- Error recovery and adaptation
- Transparent decision-making traces
- Self-reflection on progress

## Key Features

- **Interactive Examples**: Each concept includes side-by-side comparisons showing how different prompts affect model responses
- **Helper Functions**: Pre-built functions for testing prompts and displaying comparisons
- **Real-world Scenarios**: Practical examples relevant to building AI agents
- **Practice Exercises**: Hands-on activities to reinforce learning

## Learning Objectives

After completing these notebooks, you will be able to:

### General Skills
1. Understand the fundamentals of prompt engineering for AI models
2. Design effective prompts for different use cases
3. Control model output through temperature and constraints
4. Iterate and refine prompts for optimal results

### Agentic RAG Skills
5. Build RAG systems with autonomous decision-making capabilities
6. Implement query decomposition and reformulation strategies
7. Create self-reflecting retrieval systems
8. Apply corrective and adaptive retrieval patterns

### Deep Reasoning Skills
9. Apply Chain-of-Thought reasoning to complex problems
10. Implement Tree of Thoughts for exploring multiple solution paths
11. Use Self-Consistency to improve answer accuracy
12. Break down complex problems using Least-to-Most prompting
13. Build self-verification systems

### ReAct Framework Skills
14. Implement the ReAct (Reasoning and Acting) pattern
15. Build agents that use tools autonomously
16. Handle errors and recovery in agent systems
17. Create transparent, explainable agent behaviors

## Troubleshooting

### Virtual Environment Issues
- Make sure Python 3.8+ is installed: `python3 --version`
- If activation fails, try: `python3 -m venv --clear .venv`

### Connection Issues
- Verify your `.env` file has the correct values
- Ensure Azure credentials are configured (run `az login` if using Azure CLI)
- Check that your model deployment is active in Azure AI Foundry

### Jupyter Issues
- If Jupyter doesn't start, ensure it's installed: `pip install jupyter`
- Try using JupyterLab instead: `jupyter lab prompt-engineering.ipynb`

## Additional Resources

### General Resources
- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [OpenAI Best Practices for Prompt Engineering](https://platform.openai.com/docs/guides/prompt-engineering)
- [Prompt Engineering Guide](https://www.promptingguide.ai/)

### Agentic RAG Resources
- [Agentic Retrieval in Azure AI Search](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/introducing-agentic-retrieval-in-azure-ai-search/4414677)
- [Azure AI Search Documentation](https://learn.microsoft.com/azure/search/)
- [RAG Patterns and Best Practices](https://learn.microsoft.com/azure/ai-studio/concepts/retrieval-augmented-generation)

### Deep Reasoning Resources
- [Chain-of-Thought Prompting Paper](https://arxiv.org/abs/2201.11903)
- [Tree of Thoughts Paper](https://arxiv.org/abs/2305.10601)
- [Self-Consistency Improves Chain of Thought](https://arxiv.org/abs/2203.11171)
- [Least-to-Most Prompting Paper](https://arxiv.org/abs/2205.10625)

### ReAct Framework Resources
- [ReAct: Synergizing Reasoning and Acting in Language Models](https://arxiv.org/abs/2210.03629)
- [Microsoft Agent Framework](https://learn.microsoft.com/azure/ai-studio/)
- [Azure AI Foundry Agents](https://learn.microsoft.com/azure/ai-studio/concepts/agents)

### Related Course Materials
- See `demos/01-essentials/02-agentic-ai/02-rag/` for production RAG implementations
- See `demos/01-essentials/02-agentic-ai/03-evaluations/` for evaluation techniques
- See `demos/03-agent-framework/` for Microsoft Agent Framework examples

## Contributing

This is a training material. If you find issues or have suggestions for improvements, please discuss with the course instructor.
