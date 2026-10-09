# Light Remembers

## Project

- Project name: Light Remembers
- Unity version: 6000.6.5f1
- Rendering: Universal Render Pipeline (URP)
- Language: C#
- Root namespace: `LightRemembers`

## Unity rules

- Use the Unity Input System.
- Never implement gameplay using legacy `UnityEngine.Input`.
- Use Cinemachine 3 where appropriate.
- Prefer Unity CLI, Pipeline, Editor APIs, and custom Editor tooling for scene manipulation.
- Avoid manually editing Unity scene or prefab YAML.
- Preserve all `.meta` files and never regenerate GUIDs unnecessarily.
- Never leave the project with compile errors.
- Wait for Unity compilation after changing scripts.
- Check the Unity Console after significant changes.
- Run relevant tests after implementation.

## Project structure

- All game-owned content belongs under `Assets/_Game`.
- Editor-only code belongs in Editor folders or Editor assemblies.
- Third-party assets must remain separate from game-owned code.
- Do not modify `Packages` or `ProjectSettings` unnecessarily.

## Platform requirements

Design systems with Windows keyboard/mouse, gamepad, future Android touch controls, and future console support in mind.

## Performance

- Avoid unnecessary allocations in `Update` / `FixedUpdate`.
- Avoid expensive object searches during gameplay.
- Cache component references.
- Prefer event-driven systems where appropriate.
- Do not prematurely optimize simple code.

## Dependencies

Never add third-party dependencies without explicit approval.

## Git

- Never commit automatically.
- Never push automatically.
- The user reviews changes before commits.
