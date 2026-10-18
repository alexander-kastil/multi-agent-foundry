---
slide: "02-02"
topic: 02-prompt-injection
title: "Attack results per layer"
subtitle: "Four attacks from tests.json against three layers"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/02-02-attack-results.svg
order: 6
source: demos/08-governance/02-prompt-injection/demo-prompt-injection.md
gamma_prompt: >
  Slide titled "Attack results per layer", subtitle "Four attacks from tests.json
  against three layers". Left third: four short bullets on the input scan, the
  platform filter and the results of each layer. Right two thirds: a results grid
  with four attack cases as rows and the columns Input scan, Layer 1, Layer 2 and
  Layer 3; green DEFENDED cells, one red COMPROMISED cell in Layer 1, amber risk=high
  cells. Clean minimal style, white background, show the whole grid without cropping.
---

# Attack results per layer

## Content

- The **input scan** rates two of the four attacks as high risk
- The **platform filter** blocks the direct override in every layer
- Layer 1 defends **3 of 4**: the model lists its own rules
- Layers 2 and 3 defend **all four** cases

## Speaker Notes

This is the Python run from the demo guide, one row per attack case and one column per layer. Models vary, so your own baseline can look different.

Input scan: the direct override and the hidden text layer match a pattern, while the other two carry no listed phrase and score low. A scan can raise caution only for attacks it recognizes.

Platform filter: the jailbreak filter stops the direct override before the model sees it, in all three layers.

3 of 4: the baseline model resisted two document attacks on its own and obeyed the reviewer comment that asked it to list its rules. The C# run of the same layer defended all four, and a narrow pass like that is a reason to widen the attack set.

All four: with delimiters and the hardened prompt the model reports the suspicious sentence instead of following it. The layer 3 checks cost nothing while the layers above hold, and they name the failure when one does not.
