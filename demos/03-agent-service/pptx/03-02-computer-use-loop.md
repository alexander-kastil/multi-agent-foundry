---
slide: "03-02"
topic: 03-browser-automation
title: "The Computer Use loop"
subtitle: "Your application executes every action the model proposes"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/03-02-computer-use-loop.svg
order: 9
source: demos/03-agent-service/03-browser-automation/readme.md
gamma_prompt: >
  Slide titled "The Computer Use loop", subtitle "Your application executes every action the
  model proposes". Left half: four short bullets on the computer-use-preview model and its
  access request, one action per computer_call, executing it in a sandbox and returning a
  screenshot as computer_call_output, and pausing for a person on pending safety checks.
  Right half: the supplied loop diagram of agent, your application, sandbox and
  computer_call_output with a safety review branch, shown whole without cropping. Clean
  minimal style, white background.
---

# The Computer Use loop

## Content

- The agent runs on `computer-use-preview`, which needs an **access request** before you can deploy it
- Each `computer_call` holds **one action**: click, type, scroll or screenshot
- Your code runs it in a **sandbox** and sends a new screenshot back as `computer_call_output`
- `pending_safety_checks` pause the loop until **a person** approves

## Speaker Notes

With Browser Automation the service runs the browser; with Computer Use your application owns the loop. The dot travels once around the loop per iteration.

Access request: you register through an access form, and Microsoft grants access on eligibility before you can create a deployment. Step 6 of the demo shows the SpecialFeatureOrQuotaIdRequired error that a subscription without access gets.

One action: the model works from screenshots, so it sees raw pixels instead of page structure. Every computer_call item in the output holds one action for your code to carry out.

Sandbox: run the action in a place such as Playwright in a virtual machine with no access to sensitive data. Send the new screenshot back as computer_call_output, linked with previous_response_id, until the model stops asking or your iteration limit is reached.

Safety checks: pending_safety_checks mean the service detected a risk such as prompt injection or a sensitive domain. Send acknowledged_safety_checks with the next output only after a person approves.
