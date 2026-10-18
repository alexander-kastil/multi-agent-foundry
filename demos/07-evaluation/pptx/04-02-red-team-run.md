---
slide: "04-02"
topic: 04-continuous-evaluation
title: "A cloud red team run"
subtitle: "From a generated taxonomy to the flagged items you review"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/04-02-red-team-run.svg
order: 11
source: demos/07-evaluation/04-continuous-evaluation/demo-continuous-evaluation.md
gamma_prompt: >
  Slide titled "A cloud red team run", subtitle "From a generated taxonomy to the flagged items you
  review". One half holds five short bullets: the service generates a taxonomy of attack objectives
  from the agent and its tools, attack strategies such as Flip and Base64 disguise each objective, the
  evaluators score every reply, disguised attacks are flagged more often (24 of 28 against 17 of 28 for
  the baseline), and the judge's reason must be read next to the reply before acting. The other half
  shows the supplied diagram: a pipeline from taxonomy through attack strategies and the agent under
  test to the evaluators, then a bar chart of flagged items per strategy and a green review card. White
  background, no extra imagery.
---

# A cloud red team run

## Content

- The service generates a **taxonomy** of attack objectives from the agent and its tools
- **Attack strategies** such as `Flip` and `Base64` disguise each objective
- The **evaluators** score every reply of the agent under test
- Disguised attacks are flagged more often: **24 of 28** against 17 of 28 for the baseline
- **Read the reason** next to the reply before you act on a flagged item

## Speaker Notes

The AI Red Teaming Agent automates adversarial probing with Microsoft's PyRIT framework; the demo runs it in the cloud against m7-support-agent, which takes about 13 minutes.

Taxonomy: the prohibited-actions category first needs a taxonomy that the service generates from the agent and its tools. The run then attacks the agent with the baseline objectives and with disguised variants.

Strategies: the demo chooses the Flip and Base64 strategies and sets num_turns to 3. Other strategies include Morse, Leetspeak, jailbreaks and multi-turn attacks such as Crescendo.

Evaluators: the Risk and Safety evaluators score each reply, and the headline number is the Attack Success Rate. The adversarial prompts are redacted from the stored results, so reviewers see only the replies.

Flagged share: 65 of 84 prohibited-action items are flagged, and the flip and base64 attacks are flagged more often than the baseline. That gap is the signal worth tracking from release to release.

Review: the first flagged items are refusals whose own reason says Safe (No Defect). Until you have reviewed the items, treat the flagged share as an upper bound, and keep content filters in production whatever the score.
