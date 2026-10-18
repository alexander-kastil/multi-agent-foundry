---
slide: "03-02"
topic: 03-foundry-sdk
title: "The function calling loop"
subtitle: "By hand in Python, as middleware in C#"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/03-02-function-calling-loop.svg
order: 9
source: demos/01-foundry-essentials/03-foundry-sdk/demo-foundry-sdk.md
gamma_prompt: >
  Slide titled "The function calling loop", subtitle "By hand in Python, as middleware in
  C#". Left half holds five short bullets: the model returns a function_call item with a
  call_id; your code runs the function, here lookup_weather; a second responses.create
  sends a function_call_output item with the same call_id; the answer comes from the data
  the function returned; in C# UseFunctionInvocation() runs the same loop and
  AIFunctionFactory.Create turns a method into the tool. Right half shows the supplied
  diagram: a sequence between your code and the model on the left and the C# middleware
  code on the right. Clean and light, white background.
---

# The function calling loop

## Content

- The model returns a **function_call** item with a `call_id`
- Your code runs the function, here `lookup_weather`
- A second `responses.create` sends a **function_call_output** item with the same `call_id`
- The answer comes from the data your function returned
- In C#, **UseFunctionInvocation()** runs the same loop and `AIFunctionFactory.Create` turns a method into the tool

## Speaker Notes

Tool calling is a loop you own in Python and a piece of middleware in C#. The left side is step 3 of the demo, the right side step 6.

function_call: the model returns a function_call item with arguments it extracted from the question. The tool call line in the output proves it asked for lookup_weather with Vienna as the city.

Run the function: your code parses the arguments and calls lookup_weather, which returns sunny and 21 degrees.

function_call_output: a second responses.create sends the result back as a function_call_output item with the same call_id, together with the earlier output items.

Answer: the final answer comes from the returned data, since the model has no weather knowledge of its own at this point.

Middleware: in C#, ChatClientBuilder with UseFunctionInvocation runs the loop you wrote by hand, and AIFunctionFactory.Create turns LookupWeather into the tool definition. The loop code disappears and only the tool and the middleware remain.
