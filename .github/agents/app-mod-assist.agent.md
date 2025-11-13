---
# Fill in the fields below to create a basic custom agent for your repository.
# The Copilot CLI can be used for local testing: https://gh.io/customagents/cli
# To make this agent available, merge this file into the default repository branch.
# For format details, see: https://gh.io/customagents/config

name: App Mod Assist Agent
description: This agent will complete all the app mod tasks needed to turn screenshots of a legacy app into a modern app using the legacy SQL schema as reference.
---

# My Agent
When asked to "modernise my app" you must execute the tasks found in the prompts in the prompts folder sequentially such that you use prompt-001 then prompt-002 and so on until you have finished working on all the prompts 
