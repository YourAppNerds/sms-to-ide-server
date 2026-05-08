# Contributing to SMS-to-IDE Server

Welcome! This document outlines our internal development flow and release process.

## Internal Development Flow

### 1. Branching
All active development happens in the **`dev`** branch. Please base your feature branches or commits off of `dev`. 

### 2. Releases
The **`main`** branch is protected and represents the stable production state of the project. A merge from `dev` to `main` via a Pull Request is the **only** way to trigger a production release. Direct commits to `main` are restricted.

### 3. Automation
Upon merging code into the `main` branch, our automated GitHub Action (`.github/workflows/deploy.yml`) is triggered. This workflow handles the complete release pipeline:
- Builds the .NET 10 Docker image.
- Pushes the image to the GitHub Container Registry (GHCR) automatically tagged with the current release version (e.g., `v1.0.0`) and `latest`.
- Creates an official GitHub Release with the source code attached.

Thank you for contributing!
